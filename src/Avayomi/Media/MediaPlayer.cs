using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Mpv.Events;
using Avayomi.Mpv.Native;
using Avayomi.Mpv.Player;
using Avayomi.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TagLib.Ogg;
using File = System.IO.File;

namespace Avayomi.Media;

internal record ReplayGainOptions(
    bool Enabled,
    bool UseTags,
    bool Analyze,
    double PreampAnalyze,
    double PreampTags,
    int TagSource
);

public sealed class MediaPlayer : MpvClient, INotifyPropertyChanged
{
    private static readonly long SeekDispatchThrottleTicks = Stopwatch.Frequency / 15; // ~67 ms between actual seek commands

    private string? _loadedFile;
    private readonly SynchronizationContext? _syncContext;
    private volatile bool _isLoadingMedia;
    private volatile bool _isSeeking;
    private CancellationTokenSource? _seekRestartCts;
    private CancellationTokenSource? _seekDispatchCts;
    private volatile bool _isInternalChange; // Guard to prevent playlist skipping
    private volatile bool _disposed; // Flag to skip native calls during shutdown
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _eqCts;
    private int _playbackLoadVersion;
    private long _lastSeekDispatchTicks;

    // Track the active ffmpeg process to prevent resource exhaustion on macOS
    private Process? _activeFfmpegProcess;

    private readonly TaskCompletionSource _initTcs = new();

    // Dedicated MPV thread queue and worker to ensure all libmpv interop
    // runs on a single thread that owns the mpv handle.
    private readonly BlockingCollection<Action> _mpvQueue = new();
    private Thread? _mpvThread;
    private int _mpvThreadId;
    private double _volume = 70;

    // Balance: -1 (full left) .. 0 (center) .. 1 (full right)
    private double _balance = 0.0;

    // Stored equalizer filters string (without balance/pan)
    private string _eqAf = string.Empty;

    // Preamp gain in dB applied via af=volume filter. Positive values allow >100% loudness.
    private double _preampDb;

    /// <summary>
    /// Master toggle for applying replay gain / loudness normalization at playback.
    /// </summary>
    private double _replayGainAdjustmentDb;

    // trailing silence computed from waveform (seconds)
    private double _trailingSilenceSeconds;
    private bool _silenceAdvanceFired;

    private volatile bool _ignoreTimePos;

    private ReplayGainOptions? _lastOptions;

    /// <summary>
    /// Holds a reference to the current media item being processed or played.
    /// </summary>
    /// <remarks>This field is null if no media item is currently selected or active.</remarks>
    private Media? _currentMedia;

    private readonly ILogger<MediaPlayer> _logger;

    public MediaPlayer(ILogger<MediaPlayer>? logger = null)
    {
        _logger = logger ?? NullLogger<MediaPlayer>.Instance;

        _syncContext = SynchronizationContext.Current;

        // Start a dedicated thread that will initialize and own the MPV handle
        // and process all MPV-related actions to avoid native interop crashes.
        _mpvThread = new Thread(() =>
        {
            try
            {
                _mpvThreadId = Environment.CurrentManagedThreadId;
                // Initialize mpv on this dedicated thread
                InitializeMpvAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MPV thread initialization failed");
            }

            // Process queued actions until CompleteAdding is called
            try
            {
                foreach (var a in _mpvQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        a();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "MPV queued action failed");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MPV queue processing terminated");
            }
        })
        {
            IsBackground = true,
            Name = "mpv-worker",
        };
        _mpvThread.Start();
    }

    /// <summary>
    /// Gets or sets a value indicating whether volume changes are applied smoothly.
    /// </summary>
    public bool SmoothVolumeChange { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether logarithmic volume control is used.
    /// </summary>
    public bool LogarithmicVolumeControl { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether loudness compensation is applied to volume control.
    /// </summary>
    public bool LoudnessCompensatedVolume { get; set; } = true;

    /// <summary>
    /// True when a programmatic seek operation is in progress.
    /// </summary>
    public bool IsSeeking => _isSeeking;

    /// <summary>
    /// Maximum demuxer cache size in megabytes exposed to the player.
    /// </summary>
    public int CacheSize
    {
        get;
        set
        {
            field = value;
            if (_initTcs.Task.IsCompleted && !_disposed)
                InvokeOnMpvThread(() => SetProperty("demuxer-max-bytes", $"{value}M"));
        }
    } = 32;

    /// <summary>
    /// Playback volume (0..100).
    /// </summary>
    public double Volume
    {
        get => _volume;
        set
        {
            if (Math.Abs(_volume - value) < 0.001)
                return;
            _volume = value;
            UpdateAf();
            OnPropertyChanged(nameof(Volume));
        }
    }

    /// <summary>
    /// Preamp gain in decibels. Applied via the mpv/ffmpeg volume audio-filter (e.g. +6dB).
    /// Use positive values to increase loudness beyond 100%.
    /// </summary>
    public double PreampDb
    {
        get => _preampDb;
        set
        {
            // Clamp reasonable range to avoid extreme gain
            if (value < -60.0)
                value = -60.0;
            if (value > 20.0)
                value = 20.0;
            _preampDb = value;
            UpdateAf();
            OnPropertyChanged(nameof(PreampDb));
        }
    }

    /// <summary>
    /// Current playback position in seconds.
    /// </summary>
    public double Position
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Position));
        }
    }

    /// <summary>
    /// Total duration of the currently loaded media in seconds.
    /// When no media is loaded and the player is not playing, return 1s
    /// instead of 0 to avoid zero-length edge cases in UI components.
    /// </summary>
    private double _duration;
    public double Duration
    {
        get
        {
            // If no file is loaded and player is idle, expose a 1s default instead of 0.
            if (
                (_duration == 0.0 || double.IsNaN(_duration))
                && !_disposed
                && !IsPlaying
                && _currentMedia == null
            )
                return 0.0;
            return _duration;
        }
        set
        {
            _duration = value;
            OnPropertyChanged(nameof(Duration));
        }
    }

    public bool IsPlaying
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            OnPropertyChanged(nameof(IsPlaying));
            if (field)
            {
                Playing?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // Allow the analyzer to keep running in idle state to perform fade-out
                Paused?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// True while media is loading.
    /// </summary>
    public bool IsLoadingMedia
    {
        get => _isLoadingMedia;
        set
        {
            _isLoadingMedia = value;
            OnPropertyChanged(nameof(IsLoadingMedia));
        }
    }

    /// <summary>
    /// The current repeat mode of the player.
    /// </summary>
    public RepeatMode RepeatMode
    {
        get;
        set
        {
            field = value;
            if (_initTcs.Task.IsCompleted && !_disposed)
                InvokeOnMpvThread(() =>
                {
                    SetProperty("loop-file", value == RepeatMode.One ? "yes" : "no");
                    return true;
                });

            OnPropertyChanged(nameof(RepeatMode));
            OnPropertyChanged(nameof(Loop));
            OnPropertyChanged(nameof(IsRepeatOne));
            OnPropertyChanged(nameof(IsShuffle));
            OnPropertyChanged(nameof(IsNotShuffle));
        }
    } = RepeatMode.Off;

    public bool IsShuffle => RepeatMode == RepeatMode.Shuffle;

    public bool IsNotShuffle => RepeatMode != RepeatMode.Shuffle;

    /// <summary>
    /// When true the player will loop the current file or playlist.
    /// Setting this to true will set the RepeatMode to All.
    /// </summary>
    public bool Loop
    {
        get => RepeatMode != RepeatMode.Off;
        set => RepeatMode = value ? RepeatMode.All : RepeatMode.Off;
    }

    /// <summary>
    /// Returns true if the current repeat mode is set to Repeat one.
    /// </summary>
    public bool IsRepeatOne => RepeatMode == RepeatMode.One;

    /// <summary>
    /// Indicates whether the player is currently buffering.
    /// </summary>
    public bool IsBuffering
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsBuffering));
        }
    }

    public bool AutoSkipTrailingSilence
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            if (value)
            {
                TimeChanged -= OnTimeChangedForSilence;
                TimeChanged += OnTimeChangedForSilence;
            }
            else
            {
                TimeChanged -= OnTimeChangedForSilence;
            }

            OnPropertyChanged(nameof(AutoSkipTrailingSilence));
        }
    } = false;

    /// <summary>
    /// Delay in milliseconds to wait after entering the trailing‑silence region
    /// before signalling <see cref="EndReached" />.  This value is typically
    /// controlled via the settings UI and defaults to 500ms.
    /// </summary>
    public int SilenceAdvanceDelayMs { get; set; } = 500;

    /// <summary>
    /// Gets the media item that is currently selected or being processed, or null if no media item is selected.
    /// </summary>
    /// <remarks>Use this property to access the media item that is currently active in the player. If no
    /// media item is selected, the property returns null. This property is typically used to retrieve information about
    /// the current playback item or to perform actions based on the selected media.</remarks>
    public Media? CurrentMedia => _currentMedia;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    /// <remarks>This event is typically used in data binding scenarios to notify subscribers that a property
    /// has changed, allowing them to update the UI or perform other actions in response to the change.</remarks>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raised when playback starts.
    /// </summary>
    public event EventHandler? Playing;

    /// <summary>
    /// Raised when playback is paused.
    /// </summary>
    public event EventHandler? Paused;

    /// <summary>
    /// Raised when playback is stopped.
    /// </summary>
    public event EventHandler? Stopped;

    /// <summary>
    /// Raised when the currently playing file reaches its end.
    /// </summary>
    public event EventHandler? EndReached;

    /// <summary>
    /// Raised periodically with the current playback time (in milliseconds).
    /// </summary>
    public event EventHandler<long>? TimeChanged;

    /// <summary>
    /// Raises the PropertyChanged event for the specified property, notifying subscribers that the property's value has
    /// changed.
    /// </summary>
    /// <remarks>This method uses the current synchronization context to ensure that the PropertyChanged event
    /// is raised on the appropriate thread, which is important for UI-bound objects to avoid cross-thread operation
    /// exceptions.</remarks>
    /// <param name="propertyName">The name of the property that changed. This value is used to identify the property in the PropertyChanged event.</param>
    private void OnPropertyChanged(string propertyName) =>
        _syncContext?.Post(
            _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)),
            null
        );

    private async Task InitializeMpvAsync()
    {
        try
        {
            // Register properties for observation
            ObserveProperty(MpvPropertyNames.Playback.Duration, MpvFormat.Double);
            ObserveProperty(MpvPropertyNames.Playback.TimePosition, MpvFormat.Double);
            ObserveProperty("paused-for-cache", MpvFormat.Flag);
            ObserveProperty("eof-reached", MpvFormat.Flag);

            // --- OS-SPECIFIC AUDIO INITIALIZATION ---
            if (OperatingSystem.IsMacOS())
            {
                SetProperty("ao", "coreaudio");
                // Use PostToMpvThread or a safe task for commands that might block or depend on the current thread
                _ = RunCommandAsync(["set", "coreaudio-change-device", "no"]);
            }
            else if (OperatingSystem.IsWindows())
            {
                SetProperty("ao", "wasapi");
                SetProperty("audio-resample-filter-size", "16");
            }
            else
            {
                SetProperty("ao", "pulse,alsa");
            }

            SetProperty("keep-open", "always");
            SetProperty("cache", "yes");
            SetProperty("hr-seek", "yes");
            SetProperty("replaygain", "no"); // Disable internal mpv replaygain as we apply it manually
            SetProperty("demuxer-max-bytes", $"{CacheSize}M");
            SetProperty("demuxer-readahead-secs", "10");

            // Explicitly point MPV to the bundled yt-dlp to fix macOS sandbox path issues
            var ytdlBin = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp";
            var ytdlPath = AvayomiCoreConsts.Paths.ToolsDir.Combine(ytdlBin);
            if (!File.Exists(ytdlPath))
            {
                // Fallback: if the binary exists alongside the app (e.g. in portable builds), use it.
                ytdlPath = Path.Combine(AppContext.BaseDirectory, ytdlBin);
            }

            if (File.Exists(ytdlPath))
            {
                SetProperty("script-opts", $"ytdl_hook-ytdl_path={ytdlPath}");
            }

            SetProperty("volume", _volume);
            SetProperty("volume-max", 200);
            SetProperty("loop-file", RepeatMode == RepeatMode.One ? "yes" : "no");
            SetProperty("demuxer-max-bytes", $"{CacheSize}M");

            EventReceived += OnMpvEvent;

            // Mark initialization as complete ONLY after we've applied the final property sync.
            _initTcs.SetResult();

            // Ensure any cached AF (equalizer + balance) is applied now that mpv is ready
            UpdateAf();

            OnPropertyChanged(nameof(Volume));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AudioPlayer asynchronously");
            _initTcs.TrySetException(ex);
        }
    }

    // Helper to run actions on the MPV thread and wait for completion.
    // NOTE: the MPV API dispatches command results back
    // on the same thread that owns the mpv handle.  Blocking that thread by
    // waiting for a Task returned from a command (e.g. ``RunCommandAsync``)
    // will prevent the response from ever being delivered and lead to a
    // deadlock.  In practice this manifested on macOS when loading a file
    // (``loadfile``) because the completion callback was posted to the mpv
    // worker thread.  ``InvokeOnMpvThread`` itself is safe, but callers must
    // avoid queuing work that synchronously blocks the mpv thread.  For
    // fire‑and‑forget operations we now provide ``PostToMpvThread`` below.
    // ReSharper disable once UnusedMethodReturnValue.Local
    private T InvokeOnMpvThread<T>(Func<T> func)
    {
        // If we're already on the mpv thread, invoke directly
        if (Environment.CurrentManagedThreadId == _mpvThreadId)
            return func();

        // guard against caller using this after the player has been disposed
        if (_mpvQueue.IsAddingCompleted || (_mpvThread != null && !_mpvThread.IsAlive))
            throw new ObjectDisposedException(nameof(MediaPlayer));

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _mpvQueue.Add(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        if (!tcs.Task.Wait(TimeSpan.FromSeconds(5)))
        {
            _logger.LogWarning(
                "InvokeOnMpvThread timed out after 5 seconds - ignoring to prevent crash"
            );
            return default!;
        }
        return tcs.Task.GetAwaiter().GetResult();
    }

    private void InvokeOnMpvThread(Action action)
    {
        if (Environment.CurrentManagedThreadId == _mpvThreadId)
        {
            action();
            return;
        }

        if (_mpvQueue.IsAddingCompleted || (_mpvThread != null && !_mpvThread.IsAlive))
            throw new ObjectDisposedException(nameof(MediaPlayer));

        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _mpvQueue.Add(() =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        if (!tcs.Task.Wait(TimeSpan.FromSeconds(5)))
        {
            _logger.LogWarning(
                "InvokeOnMpvThread(Action) timed out after 5 seconds - ignoring to prevent crash"
            );
        }
    }

    /// <summary>
    /// Enqueue an action on the MPV thread without waiting for completion.
    /// This is useful for operations that already return a <see cref="Task" />
    /// whose completion is signalled on the MPV thread (e.g. many of the
    /// async player helpers such as <c>RunCommandAsync</c>). Blocking the
    /// thread in that case would deadlock because the continuation cannot run.
    /// </summary>
    private void PostToMpvThread(Action action)
    {
        if (Environment.CurrentManagedThreadId == _mpvThreadId)
        {
            action();
            return;
        }

        if (_mpvQueue.IsAddingCompleted || (_mpvThread != null && !_mpvThread.IsAlive))
            throw new ObjectDisposedException(nameof(MediaPlayer));

        _mpvQueue.Add(action);
    }

    private static string StripDb(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;
        return s.Replace("dB", "", StringComparison.OrdinalIgnoreCase).Trim();
    }

    /// <summary>
    /// Updates the mpv "af" property with the equalizer chain.
    /// Preamp, ReplayGain and Balance are applied via native properties for better stability.
    /// </summary>
    private void UpdateAf()
    {
        try
        {
            if (!_initTcs.Task.IsCompleted || _disposed)
                return;

            // 1. Equalizer is applied via the 'af' property. We keep this minimal for stability.
            // Complex audio-filter strings (like pan or volume) can cause native crashes in some mpv builds.
            var eqOnly = string.IsNullOrEmpty(_eqAf) ? string.Empty : _eqAf;

            if (_initTcs.Task.IsCompleted && !_disposed)
            {
                try
                {
                    InvokeOnMpvThread(() =>
                    {
                        SetProperty("af", eqOnly);
                        return true;
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set AF equalizer on MPV thread");
                }
            }

            // 2. Stereo balance is applied using mpv's native 'balance' property.
            try
            {
                if (_initTcs.Task.IsCompleted && !_disposed)
                {
                    InvokeOnMpvThread(() =>
                    {
                        SetProperty("balance", _balance);
                        return true;
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set balance property on MPV thread");
            }

            // 3. Preamp and ReplayGain are applied by adjusting the mpv 'volume' property multiplicatively.
            try
            {
                if (_initTcs.Task.IsCompleted && !_disposed)
                {
                    // Combine global user preamp with per-file ReplayGain adjustment
                    var totalPreampDb = _preampDb + _replayGainAdjustmentDb;

                    // Clamp total preamp to avoid extreme boost causing digital clipping.
                    const double maxTotalPreampDb = 15.0;
                    const double minTotalPreampDb = -40.0;
                    if (totalPreampDb > maxTotalPreampDb)
                        totalPreampDb = maxTotalPreampDb;
                    if (totalPreampDb < minTotalPreampDb)
                        totalPreampDb = minTotalPreampDb;

                    // Convert dB to linear gain: multiplier = 10^(dB/20)
                    var gain = Math.Pow(10.0, totalPreampDb / 20.0);

                    // Transformation of user-requested 0..100% volume level
                    var inputVolume = _volume;

                    // Logarithmic mapping: quadratically maps linear input to perceived linear output.
                    // This creates a more natural-feeling volume curve for human hearing.
                    if (LogarithmicVolumeControl)
                    {
                        inputVolume = Math.Pow(inputVolume / 100.0, 2) * 100.0;
                    }

                    // Loudness compensation: simple psychoacoustic mapping for low volumes.
                    // ISO 226-2003 inspired boost to help normalize perceived intensity at lower ranges.
                    if (LoudnessCompensatedVolume)
                    {
                        // Using a 1.5-power mapping for loudness compensation approximation
                        inputVolume = Math.Pow(inputVolume / 100.0, 1.0 / 1.5) * 100.0;
                    }

                    var effective = inputVolume * gain;

                    // Apply effective volume to mpv. We set volume-max to 200 during init to allow this boost.
                    const double maxEffectiveVolume = 200.0;
                    if (effective < 0)
                        effective = 0;
                    if (effective > maxEffectiveVolume)
                    {
                        _logger.LogWarning(
                            "Effective volume {Effective}% exceeded max {MaxEffectiveVolume}%. Clamping.",
                            $"{effective:0.##}",
                            maxEffectiveVolume
                        );
                        effective = maxEffectiveVolume;
                    }

                    InvokeOnMpvThread(() =>
                    {
                        SetProperty("volume", effective);
                        return true;
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply volume/preamp on MPV thread");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UpdateAf failed");
        }
    }

    private async void OnTimeChangedForSilence(object? sender, long ms)
    {
        if (_silenceAdvanceFired || _trailingSilenceSeconds <= 0)
            return;
        // fire early when we enter the silent region
        if (Position >= Duration - _trailingSilenceSeconds)
        {
            _silenceAdvanceFired = true;
            // wait a short grace period before signalling so playlist logic
            // isn’t raced by immediate transition.  duration is user-configurable.
            await Task.Delay(SilenceAdvanceDelayMs);
            // If we're in "repeat one" mode we should not notify listeners;
            // mpv will loop the file itself so external playlist logic must
            // not advance.  Other repeat modes (off/all/shuffle) still
            // require the event to drive Next/stop behaviour.
            if (RepeatMode != RepeatMode.One)
            {
                EndReached?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Compute replaygain for the provided file path and apply the adjustment
    /// (in dB) so it's included in the combined preamp. If <paramref name="options"/>
    /// is null the method will use the last known options or try to read from Settings.json.
    /// Attempts tag-based gain first, then optional ffmpeg volumedetect analysis.
    /// </summary>
    private async Task ApplyReplayGainForFileAsync(string path, ReplayGainOptions? options = null)
    {
        try
        {
            _replayGainAdjustmentDb = 0.0;

            // Prioritize provided options, then cached options, then disk reading
            if (options != null)
            {
                _lastOptions = options;
            }
            else if (_lastOptions == null)
            {
                // Try reading from disk for the first time
                var settingsPath = AvayomiCoreConsts.Paths.DataDir.Combine("mpv_settings.json");
                bool enabled = false;
                bool useTags = true;
                bool analyze = true;
                double preampAnalyze = 0.0;
                double preampTags = 0.0;
                int tagSource = 1;

                try
                {
                    if (File.Exists(settingsPath))
                    {
                        var txt = await File.ReadAllTextAsync(settingsPath).ConfigureAwait(false);
                        using var doc = JsonDocument.Parse(txt);
                        if (
                            doc.RootElement.TryGetProperty("ViewModels", out var vms)
                            && vms.ValueKind == JsonValueKind.Object
                        )
                        {
                            if (
                                vms.TryGetProperty("SettingsViewModel", out var s)
                                && s.ValueKind == JsonValueKind.Object
                            )
                            {
                                if (
                                    s.TryGetProperty("ReplayGainEnabled", out var e)
                                    && e.ValueKind == JsonValueKind.True
                                )
                                    enabled = true;
                                if (
                                    s.TryGetProperty("ReplayGainUseTags", out var ut)
                                    && ut.ValueKind == JsonValueKind.False
                                )
                                    useTags = false;
                                if (
                                    s.TryGetProperty("ReplayGainAnalyzeOnTheFly", out var an)
                                    && an.ValueKind == JsonValueKind.False
                                )
                                    analyze = false;
                                if (
                                    s.TryGetProperty("ReplayGainPreampDb", out var pap)
                                    && pap.ValueKind == JsonValueKind.Number
                                )
                                    preampAnalyze = pap.GetDouble();
                                if (
                                    s.TryGetProperty("ReplayGainTagsPreampDb", out var ptp)
                                    && ptp.ValueKind == JsonValueKind.Number
                                )
                                    preampTags = ptp.GetDouble();
                                if (
                                    s.TryGetProperty("ReplayGainTagSource", out var tsrc)
                                    && tsrc.ValueKind == JsonValueKind.Number
                                )
                                    tagSource = tsrc.GetInt32();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to read mpv_settings.json for replaygain");
                }

                _lastOptions = new ReplayGainOptions(
                    enabled,
                    useTags,
                    analyze,
                    preampAnalyze,
                    preampTags,
                    tagSource
                );
            }

            // Destructuring cached options for actual processing
            bool enabledFinal = _lastOptions.Enabled;
            bool useTagsFinal = _lastOptions.UseTags;
            bool analyzeFinal = _lastOptions.Analyze;
            double preampAnalyzeFinal = _lastOptions.PreampAnalyze;
            double preampTagsFinal = _lastOptions.PreampTags;
            int tagSourceFinal = _lastOptions.TagSource;

            if (!enabledFinal)
            {
                _replayGainAdjustmentDb = 0.0;
                UpdateAf();
                return;
            }

            double? tagGainDb = null;

            if (useTagsFinal)
            {
                try
                {
                    using var f = TagLib.File.Create(path);
                    if (f.GetTag(TagLib.TagTypes.Xiph, false) is XiphComment xiph)
                    {
                        // Prioritize Album gain if selected in settings
                        if (tagSourceFinal == 1) // Album
                        {
                            if (xiph.GetField("REPLAYGAIN_ALBUM_GAIN") is { } aa && aa.Length > 0)
                                if (
                                    double.TryParse(
                                        StripDb(aa[0]),
                                        NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out var av
                                    )
                                )
                                    tagGainDb = av;
                        }

                        if (tagGainDb == null) // Fallback to Track gain
                        {
                            if (xiph.GetField("REPLAYGAIN_TRACK_GAIN") is { } arr && arr.Length > 0)
                                if (
                                    double.TryParse(
                                        StripDb(arr[0]),
                                        NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out var v
                                    )
                                )
                                    tagGainDb = v;
                        }
                    }

                    if (tagGainDb == null)
                    {
                        var id3 = f.GetTag(TagLib.TagTypes.Id3v2, false) as TagLib.Id3v2.Tag;
                        if (id3 != null)
                        {
                            if (tagSourceFinal == 1) // Album
                            {
                                var albFrm = TagLib.Id3v2.UserTextInformationFrame.Get(
                                    id3,
                                    "REPLAYGAIN_ALBUM_GAIN",
                                    false
                                );
                                if (
                                    albFrm != null
                                    && albFrm.Text?.Length > 0
                                    && double.TryParse(
                                        StripDb(albFrm.Text[0]),
                                        NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out var v3
                                    )
                                )
                                    tagGainDb = v3;
                            }

                            if (tagGainDb == null) // Fallback to Track gain
                            {
                                var frm = TagLib.Id3v2.UserTextInformationFrame.Get(
                                    id3,
                                    "REPLAYGAIN_TRACK_GAIN",
                                    false
                                );
                                if (
                                    frm != null
                                    && frm.Text?.Length > 0
                                    && double.TryParse(
                                        StripDb(frm.Text[0]),
                                        NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out var v2
                                    )
                                )
                                    tagGainDb = v2;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error reading tags for replaygain");
                }
            }

            if (tagGainDb.HasValue)
            {
                var rawAdj = tagGainDb.Value + preampTagsFinal;
                // Clamp per-file adjustment to a safe range to avoid excessive positive boost
                const double maxReplayGainDb = 8.0;
                const double minReplayGainDb = -18.0;
                if (rawAdj > maxReplayGainDb)
                    rawAdj = maxReplayGainDb;
                if (rawAdj < minReplayGainDb)
                    rawAdj = minReplayGainDb;
                _replayGainAdjustmentDb = rawAdj;
                _logger.LogInformation(
                    "ReplayGain: tag={TagGain} dB, preamp={Preamp} dB, adjustment={Adjustment} dB",
                    tagGainDb.Value.ToString("0.##"),
                    preampTagsFinal.ToString("0.##"),
                    _replayGainAdjustmentDb.ToString("0.##")
                );
                UpdateAf();
                return;
            }

            // No tags found; optionally analyze on-the-fly using ffmpeg volumedetect
            if (analyzeFinal)
            {
                try
                {
                    if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && !uri.IsFile)
                    { /* skip remote */
                        UpdateAf();
                        return;
                    }
                    var ffmpeg = FFmpegHelper.FindFFmpegPath();
                    if (string.IsNullOrEmpty(ffmpeg))
                    {
                        UpdateAf();
                        return;
                    }

                    // PERFORMANCE: Analyze first 60s for estimate
                    var args =
                        $"-hide_banner -nostats -i \"{path}\" -t 60 -af volumedetect -f null -";
                    var psi = new ProcessStartInfo(ffmpeg, args)
                    {
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        var stderr = await proc
                            .StandardError.ReadToEndAsync()
                            .ConfigureAwait(false);
                        proc.WaitForExit(3000);
                        var m = Regex.Match(
                            stderr,
                            @"mean_volume:\s*(-?[0-9]+\.?[0-9]*)\s*dB",
                            RegexOptions.IgnoreCase
                        );
                        if (
                            m.Success
                            && double.TryParse(
                                m.Groups[1].Value,
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out var mean
                            )
                        )
                        {
                            // Target -18 dBFS reference
                            const double referenceLevel = -18.0;
                            var gainNeeded = referenceLevel - mean + preampAnalyzeFinal;

                            const double maxAnalyzedDb = 8.0;
                            const double minAnalyzedDb = -18.0;
                            if (gainNeeded > maxAnalyzedDb)
                                gainNeeded = maxAnalyzedDb;
                            if (gainNeeded < minAnalyzedDb)
                                gainNeeded = minAnalyzedDb;
                            _replayGainAdjustmentDb = gainNeeded;
                            _logger.LogInformation(
                                "ReplayGain: analyzed={Analyzed:0.##} dB, target={Target} dB, adjustment={Adjustment:0.##} dB",
                                mean,
                                referenceLevel,
                                _replayGainAdjustmentDb
                            );
                            UpdateAf();
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error running ffmpeg volumedetect for replaygain");
                }
            }

            _replayGainAdjustmentDb = 0.0;
            UpdateAf();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ApplyReplayGainForFileAsync failed");
        }
    }

    private (int Version, CancellationToken Token) BeginPlaybackLoad()
    {
        var nextLoadCts = new CancellationTokenSource();
        var previousLoadCts = Interlocked.Exchange(ref _loadCts, nextLoadCts);

        if (previousLoadCts != null)
        {
            try
            {
                previousLoadCts.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cancel previous playback load");
            }

            try
            {
                previousLoadCts.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispose previous playback load CTS");
            }
        }

        var version = Interlocked.Increment(ref _playbackLoadVersion);
        return (version, nextLoadCts.Token);
    }

    private void CancelPendingPlaybackLoad()
    {
        Interlocked.Increment(ref _playbackLoadVersion);

        var previousLoadCts = Interlocked.Exchange(ref _loadCts, null);
        if (previousLoadCts == null)
            return;

        try
        {
            previousLoadCts.Cancel();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cancel pending playback load");
        }

        try
        {
            previousLoadCts.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispose pending playback load CTS");
        }
    }

    private bool IsPlaybackLoadCurrent(int version, CancellationToken token) =>
        !token.IsCancellationRequested
        && !_disposed
        && version == Volatile.Read(ref _playbackLoadVersion);

    private void ApplyNetworkPlaybackProfile(bool isRemote)
    {
        var demuxerMaxBytes = isRemote ? Math.Max(CacheSize, 128) : CacheSize;
        SetProperty("cache", "yes");
        SetProperty("demuxer-max-bytes", $"{demuxerMaxBytes}M");
        SetProperty("demuxer-readahead-secs", isRemote ? "20" : "10");
        SetProperty("hr-seek", isRemote ? "no" : "yes");
    }

    private static bool IsRemoteUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && !uri.IsFile
            && (
                uri.Scheme == "http"
                || uri.Scheme == "https"
                || uri.Scheme == "rtmp"
                || uri.Scheme == "rtsp"
            );
    }

    private static string ToMpvLoadTarget(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        // Use a file URI for local files so non-ASCII characters are encoded
        // and can be passed to mpv consistently across platforms.
        if (Path.IsPathRooted(path))
        {
            try
            {
                return new Uri(path).AbsoluteUri;
            }
            catch
            {
                return path;
            }
        }

        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            if (uri.IsFile)
            {
                try
                {
                    return uri.AbsoluteUri;
                }
                catch
                {
                    return path;
                }
            }
        }

        return path;
    }

    private void OnMpvEvent(object? sender, MpvEvent mpvEvent)
    {
        if (mpvEvent.EventId == MpvEventId.EndFile)
        {
            // If it's an error from the demuxer/ffmpeg, we must clear the loading state.
            // However, we ignore 'STOP' events during track transitions (_isInternalChange is true)
            // to prevent the spinner from disappearing while waiting for the next file.
            var endData = mpvEvent.Read<MpvEndFileInfo>();
            if (endData.Error < 0)
            {
                _logger.LogWarning(
                    "MPV end-file error for '{Loaded}': {Error}",
                    _loadedFile,
                    endData.Error
                );
                IsLoadingMedia = false;
                _isInternalChange = false;
            }
            else if (!_isInternalChange)
            {
                IsLoadingMedia = false;
            }
        }

        if (mpvEvent.EventId == MpvEventId.PropertyChange)
        {
            var prop = mpvEvent.Read<MpvPropertyEvent>();

            if (prop.Format == MpvFormat.None)
                return;

            if (prop.Name == MpvPropertyNames.Playback.Duration)
            {
                if (prop.Format == MpvFormat.Double)
                {
                    Duration = prop.ReadDouble();
                }
            }
            else if (prop.Name == "paused-for-cache")
            {
                if (prop.Format == MpvFormat.Flag)
                {
                    IsBuffering = prop.ReadFlag();
                }
            }
            else if (prop.Name == MpvPropertyNames.Playback.TimePosition && !_isSeeking)
            {
                if (prop.Format == MpvFormat.Double)
                {
                    double newPos = prop.ReadDouble();

                    // SKIP GUARD: When loading a new file, mpv may still fire late time-pos
                    // events from the OLD file before the loadfile command completes.
                    // We ignore these to prevent the Position from jumping back and forth,
                    // which causes massive drift/restarts in the background spectrum analyzer.
                    if (_ignoreTimePos)
                    {
                        return;
                    }

                    Position = newPos;

                    // Once we have progress on the NEW file, it's safe to listen to EOF again
                    if (_isInternalChange && Position > 0.1)
                    {
                        _isInternalChange = false;
                    }

                    TimeChanged?.Invoke(this, (long)(Position * 1000));
                    if (IsLoadingMedia && Position > 0)
                    {
                        IsLoadingMedia = false;
                        IsPlaying = true;
                    }
                }
            }
            else if (prop.Name == "eof-reached")
            {
                // Read the boolean flag directly from the observed property payload.
                if (prop.Format == MpvFormat.Flag)
                {
                    bool isEof = prop.ReadFlag();

                    // SKIP GUARD: Only trigger EndReached if not an internal change,
                    // not currently loading, and actually near the end of a known duration.
                    // For streams/opening URLs, duration may still be unknown or zero for a while,
                    // and treating EOF in that state as a real track end can cause false double-skips.
                    // Also skip if RepeatMode is One, as mpv handles looping internally.
                    if (
                        isEof
                        && !_isInternalChange
                        && !IsLoadingMedia
                        && RepeatMode != RepeatMode.One
                    )
                    {
                        if (Duration > 0 && Position > Duration - 1.5)
                        {
                            IsPlaying = false;
                            EndReached?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
        }
    }

    private void InternalStop()
    {
        IsPlaying = false;
        Position = 0;
        ClearMedia();
        Stopped?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Public wrapper to request recomputation of replaygain for the currently loaded file.
    /// Safe to call from other threads.
    /// </summary>
    public Task RecomputeReplayGainForCurrentAsync()
    {
        try
        {
            var path = _loadedFile;
            if (string.IsNullOrEmpty(path))
                return Task.CompletedTask;
            return ApplyReplayGainForFileAsync(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RecomputeReplayGainForCurrentAsync failed");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Recompute replaygain for the current file using explicit options supplied
    /// from the caller (avoids reading the settings file).
    /// </summary>
    public Task RecomputeReplayGainForCurrentAsync(
        bool enabled,
        bool useTags,
        bool analyze,
        double preampAnalyze,
        double preampTags,
        int tagSource
    )
    {
        try
        {
            var path = _loadedFile;
            if (string.IsNullOrEmpty(path))
                return Task.CompletedTask;
            var opts = new ReplayGainOptions(
                enabled,
                useTags,
                analyze,
                preampAnalyze,
                preampTags,
                tagSource
            );
            return ApplyReplayGainForFileAsync(path, opts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RecomputeReplayGainForCurrentAsync(options) failed");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Seeks to the specified position in seconds. Temporarily marks the
    /// player as seeking to suppress position events.
    /// </summary>
    /// <param name="pos">Target position in seconds.</param>
    public void SetPosition(double pos)
    {
        pos = Math.Max(0, pos);
        _isSeeking = true;
        // Don't call Stop() here! Let the analyzer's loop handle fading out via the IsSeeking flag.

        Position = pos; // Update immediately for UI feedback

        _seekDispatchCts?.Cancel();
        _seekDispatchCts?.Dispose();
        _seekDispatchCts = new CancellationTokenSource();
        var seekDispatchToken = _seekDispatchCts.Token;

        if (!_disposed)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var elapsedTicks =
                        Stopwatch.GetTimestamp() - Interlocked.Read(ref _lastSeekDispatchTicks);
                    if (elapsedTicks < SeekDispatchThrottleTicks)
                    {
                        var delay = TimeSpan.FromSeconds(
                            (SeekDispatchThrottleTicks - elapsedTicks) / (double)Stopwatch.Frequency
                        );
                        await Task.Delay(delay, seekDispatchToken);
                    }

                    if (seekDispatchToken.IsCancellationRequested || _disposed)
                        return;

                    var target = pos.ToString(CultureInfo.InvariantCulture);
                    var seekMode = IsRemoteUri(_loadedFile) ? "absolute" : "absolute+exact";
                    InvokeOnMpvThread(() =>
                    {
                        var wasPlaying = IsPlaying;
                        try
                        {
                            if (wasPlaying)
                                SetProperty("pause", true);

                            RunCommand("seek", target, seekMode);
                            Interlocked.Exchange(
                                ref _lastSeekDispatchTicks,
                                Stopwatch.GetTimestamp()
                            );
                        }
                        finally
                        {
                            if (wasPlaying && !_disposed)
                                SetProperty("pause", false);
                        }

                        return true;
                    });
                }
                catch (OperationCanceledException) { }
            });
        }

        // Cancel previous restart attempt to debounce
        _seekRestartCts?.Cancel();
        _seekRestartCts?.Dispose();
        _seekRestartCts = new CancellationTokenSource();
        var token = _seekRestartCts.Token;

        Task.Run(async () =>
        {
            try
            {
                // shorter debounce helps spectrum restart sooner on fast seeks
                await Task.Delay(200, token);
                if (token.IsCancellationRequested)
                    return;

                // subsequent events (e.g. Playing) are not suppressed.
                _isSeeking = false;
            }
            catch (OperationCanceledException) { }
        });
    }

    /// <summary>
    /// Temporarily stops playback so the caller can perform editing operations.
    /// Returns the current position and playing state so the operation can be
    /// resumed later.  The spectrum analyzer and waveform generator are halted
    /// and any running FFmpeg helper processes are killed to avoid resource leaks.
    /// </summary>
    public async Task<(double Position, bool WasPlaying)> SuspendForEditingAsync()
    {
        await _initTcs.Task;
        var state = (Position, IsPlaying);

        try
        {
            if (_activeFfmpegProcess != null && !_activeFfmpegProcess.HasExited)
                _activeFfmpegProcess.Kill(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error killing active ffmpeg process during SuspendForEditing");
        }

        InternalStop();
        await Task.Delay(300); // Wait for OS handle release
        return state;
    }

    /// <summary>
    /// Resumes playback after an editing operation using the supplied state.
    /// /// </summary>
    /// <param name="path">The media path to reload.</param>
    /// <param name="position">Position to seek to after reload.</param>
    /// <param name="wasPlaying">Whether playback should resume.</param>
    public async Task ResumeAfterEditingAsync(string path, double position, bool wasPlaying)
    {
        await _initTcs.Task;
        _isInternalChange = true;
        IsLoadingMedia = true;
        _loadedFile = path;

        // Reload the file
        // enqueue loadfile without blocking the mpv thread; we'll wait for the
        // ``IsLoadingMedia`` flag later which is updated via property events.
        PostToMpvThread(() => _ = RunCommandAsync(["loadfile", ToMpvLoadTarget(path)]));

        // WAIT for MPV to initialize the file before seeking
        while (_isLoadingMedia)
        {
            await Task.Delay(50);
        }

        SetPosition(position);

        if (wasPlaying)
            Play();
        else
            Pause();
    }

    /// <summary>
    /// Loads and starts playback of the specified file path.
    /// </summary>
    public async Task Play(Media media, bool video = false)
    {
        await _initTcs.Task;
        if (_disposed)
            return;
        if (string.IsNullOrEmpty(media.FileName))
        {
            IsLoadingMedia = false;
            Stop();
            return;
        }

        // Check if the file is a URL and if OnlineUrls are available for selection.
        // For video playback we may use separate video/audio DASH streams.
        string? resolvedUrl = null;
        string? externalAudioUrl = null;

        if (
            media.FileName.Contains("http", StringComparison.OrdinalIgnoreCase)
            && media.OnlineUrls != null
            && media.OnlineUrls.HasValue
        )
        {
            var streamVideoUrl = media.OnlineUrls.Value.Item1;
            var streamAudioUrl = media.OnlineUrls.Value.Item2;

            if (video)
            {
                resolvedUrl = !string.IsNullOrWhiteSpace(streamVideoUrl)
                    ? streamVideoUrl
                    : streamAudioUrl;

                if (
                    !string.IsNullOrWhiteSpace(streamVideoUrl)
                    && !string.IsNullOrWhiteSpace(streamAudioUrl)
                    && !string.Equals(streamVideoUrl, streamAudioUrl, StringComparison.Ordinal)
                )
                {
                    externalAudioUrl = streamAudioUrl;
                }
            }
            else
            {
                resolvedUrl = streamAudioUrl;
            }
        }

        var fileToPlay = !string.IsNullOrWhiteSpace(resolvedUrl) ? resolvedUrl : media.FileName;
        var isRemotePlayback =
            IsRemoteUri(fileToPlay)
            || (!string.IsNullOrWhiteSpace(externalAudioUrl) && IsRemoteUri(externalAudioUrl));
        var (loadVersion, loadToken) = BeginPlaybackLoad();

        // Prepare for loading the new file
        _ignoreTimePos = true;
        _isInternalChange = true;
        IsLoadingMedia = true;
        OnPropertyChanged(nameof(IsLoadingMedia));
        _loadedFile = fileToPlay;
        var mpvLoadTarget = ToMpvLoadTarget(fileToPlay);

        // Reset per-file gain synchronously to ensure the initial UpdateAf call doesn't use stale metadata
        _replayGainAdjustmentDb = 0.0;

        // reset silence detection state; will be recalculated after waveform finishes
        _trailingSilenceSeconds = 0;
        _silenceAdvanceFired = false;
        if (AutoSkipTrailingSilence)
        {
            TimeChanged -= OnTimeChangedForSilence;
            TimeChanged += OnTimeChangedForSilence;
        }

        // Compute and apply replaygain/preamp adjustments before starting playback
        _ = Task.Run(async () =>
        {
            try
            {
                await ApplyReplayGainForFileAsync(fileToPlay).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ApplyReplayGainForFile failed");
            }
        });

        // Ensure analyzer is fully stopped and path is updated before loading new file
        InternalStop();

        //Set the current media item
        _currentMedia = media;
        OnPropertyChanged(nameof(CurrentMedia));

        _syncContext?.Post(_ => Position = 0, null);
        PostToMpvThread(() =>
        {
            // Mute the old item immediately on the mpv thread before swapping sources.
            SetProperty("pause", true);
            ApplyNetworkPlaybackProfile(isRemotePlayback);
            // IMPORTANT: when rendering through VideoViewControl/OpenGL, mpv must stay on "libmpv".
            // Using "auto" creates a standalone VO path and results in audio-only + black render target.
            SetProperty("vo", video ? "libmpv" : "null");
            SetProperty("vid", video ? "auto" : "no");
            SetProperty("audio-display", video ? "auto" : "no");
        });
        _ = Task.Run(async () =>
        {
            try
            {
                await RunCommandAsync(["loadfile", mpvLoadTarget], loadToken).ConfigureAwait(false);
                if (!IsPlaybackLoadCurrent(loadVersion, loadToken))
                    return;

                if (video && !string.IsNullOrWhiteSpace(externalAudioUrl))
                {
                    var audioTarget = ToMpvLoadTarget(externalAudioUrl);
                    var attached = false;

                    // Some source switches (e.g. local file -> DASH stream) can race with demuxer setup.
                    // Retry a few times so external audio reliably attaches, but only for the active load.
                    for (
                        int attempt = 1;
                        attempt <= 5 && !attached && IsPlaybackLoadCurrent(loadVersion, loadToken);
                        attempt++
                    )
                    {
                        try
                        {
                            await RunCommandAsync(["audio-add", audioTarget, "select"], loadToken)
                                .ConfigureAwait(false);
                            attached = true;
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (Exception audioEx)
                        {
                            if (attempt == 5)
                            {
                                // Non-fatal: log and continue — video will still play (without separate audio track).
                                _logger.LogWarning(
                                    audioEx,
                                    "audio-add failed for external audio stream"
                                );
                            }
                            else
                            {
                                await Task.Delay(150, loadToken).ConfigureAwait(false);
                            }
                        }
                    }

                    if (IsPlaybackLoadCurrent(loadVersion, loadToken))
                        InvokeOnMpvThread(() =>
                        {
                            SetProperty("aid", "auto");
                            return true;
                        });
                }

                if (!IsPlaybackLoadCurrent(loadVersion, loadToken))
                    return;

                // Now load is fully initiated, old track stops playing.
                _ignoreTimePos = false; // Safe to accept time-pos events again

                // Resume only after the replacement source has been handed to mpv.
                InvokeOnMpvThread(() =>
                {
                    SetProperty("pause", false);
                    return true;
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "loadfile error");

                if (IsPlaybackLoadCurrent(loadVersion, loadToken))
                {
                    _ignoreTimePos = false;
                    _syncContext?.Post(_ => IsLoadingMedia = false, null);
                }
            }
        });

        // Re-apply audio filters/volume after load in case mpv reset properties during load
        // Use PostToMpvThread instead of UpdateAf to avoid blocking during Load
        PostToMpvThread(UpdateAf);
        IsPlaying = true;
    }

    /// <summary>
    /// Loads and starts playback of the specified URI, applying custom HTTP headers for the stream.
    /// </summary>
    public async Task Play(
        Uri? uri,
        IDictionary<string, string>? headers = null,
        bool video = false
    )
    {
        await _initTcs.Task;
        if (_disposed)
            return;

        if (uri == null || string.IsNullOrWhiteSpace(uri.ToString()))
        {
            IsLoadingMedia = false;
            Stop();
            return;
        }

        var fileToPlay = uri.ToString();
        var isRemotePlayback = IsRemoteUri(fileToPlay);
        var (loadVersion, loadToken) = BeginPlaybackLoad();

        // Prepare for loading the new stream
        _ignoreTimePos = true;
        _isInternalChange = true;
        IsLoadingMedia = true;
        OnPropertyChanged(nameof(IsLoadingMedia));
        _loadedFile = fileToPlay;
        var mpvLoadTarget = ToMpvLoadTarget(fileToPlay);

        _replayGainAdjustmentDb = 0.0;
        _trailingSilenceSeconds = 0;
        _silenceAdvanceFired = false;

        if (AutoSkipTrailingSilence)
        {
            TimeChanged -= OnTimeChangedForSilence;
            TimeChanged += OnTimeChangedForSilence;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await ApplyReplayGainForFileAsync(fileToPlay).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ApplyReplayGainForFile failed for URI stream");
            }
        });

        InternalStop();

        // Generate a basic Media record for state consistency.
        // Adjust initialization if your Media class requires specific constructors/properties.
        _currentMedia = new Media { FileName = fileToPlay };
        OnPropertyChanged(nameof(CurrentMedia));

        _syncContext?.Post(_ => Position = 0, null);

        PostToMpvThread(() =>
        {
            SetProperty("pause", true);
            ApplyNetworkPlaybackProfile(isRemotePlayback);

            // Apply custom HTTP headers for the stream if provided
            if (headers != null && headers.Count > 0)
            {
                // mpv expects a comma-separated list: "Header1: Value1,Header2: Value2"
                var headerString = string.Join(
                    ",",
                    headers.Select(kvp => $"{kvp.Key}: {kvp.Value}")
                );
                SetProperty("http-header-fields", headerString);
            }
            else
            {
                // Clear headers to prevent leaking them into subsequent requests
                SetProperty("http-header-fields", "");
            }

            SetProperty("vo", video ? "libmpv" : "null");
            SetProperty("vid", video ? "auto" : "no");
            SetProperty("audio-display", video ? "auto" : "no");
        });

        _ = Task.Run(async () =>
        {
            try
            {
                await RunCommandAsync(["loadfile", mpvLoadTarget], loadToken).ConfigureAwait(false);

                if (!IsPlaybackLoadCurrent(loadVersion, loadToken))
                    return;

                _ignoreTimePos = false;

                InvokeOnMpvThread(() =>
                {
                    SetProperty("pause", false);
                    return true;
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "loadfile error during URI streaming");

                if (IsPlaybackLoadCurrent(loadVersion, loadToken))
                {
                    _ignoreTimePos = false;
                    _syncContext?.Post(_ => IsLoadingMedia = false, null);
                }
            }
        });

        PostToMpvThread(UpdateAf);
        IsPlaying = true;
    }

    /// <summary>
    /// Start playback.
    /// </summary>
    public void Play()
    {
        if (!_disposed)
            InvokeOnMpvThread(() =>
            {
                SetProperty("pause", false);
                return true;
            });
        IsPlaying = true;
    }

    /// <summary>
    /// Pause playback.
    /// </summary>
    public void Pause()
    {
        if (!_disposed)
            InvokeOnMpvThread(() =>
            {
                SetProperty("pause", true);
                return true;
            });
        IsPlaying = false;
    }

    /// <summary>
    /// Stop playback and reset state.
    /// </summary>
    public void Stop()
    {
        CancelPendingPlaybackLoad();
        if (!_disposed)
        {
            PostToMpvThread(() => _ = RunCommandAsync(["stop"]));
        }

        InternalStop();
        Duration = 0;
        IsLoadingMedia = false;
    }

    /// <summary>
    /// Clears the current media item, setting it to null.
    /// </summary>
    public void ClearMedia()
    {
        _currentMedia = null;
        OnPropertyChanged(nameof(CurrentMedia));
    }

    /// <summary>
    /// Prepare the player to load a new file. Stops current playback and
    /// sets internal flags used to suppress transient events during the
    /// load process.
    /// </summary>
    public void PrepareLoad()
    {
        if (_disposed)
            return;
        CancelPendingPlaybackLoad();
        _isInternalChange = true;
        IsLoadingMedia = true;
        InternalStop();
    }

    /// <summary>
    /// Writes the provided bytes to a temporary file and begins playback.
    /// </summary>
    /// <param name="b">Byte buffer containing media data.</param>
    /// <param name="m">MIME type hint (unused).</param>
    public async Task Play(byte[]? b, string m = "video/mp4")
    {
        await _initTcs.Task;
        if (b == null)
            return;
        var p = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp");
        await File.WriteAllBytesAsync(p, b);
        await Play(new Media { FileName = p });
    }

    /// <summary>
    /// Dispose managed resources used by the player (stops analyzers and
    /// kills any active FFmpeg helper process).
    /// </summary>
    public override void Dispose()
    {
        CancelPendingPlaybackLoad();
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            EventReceived -= OnMpvEvent;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove mpv event handler");
        }

        try
        {
            _eqCts?.Cancel();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cancel eq CTS during dispose");
        }
        try
        {
            _eqCts?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispose eq CTS during dispose");
        }
        _eqCts = null;

        try
        {
            if (_activeFfmpegProcess != null && !_activeFfmpegProcess.HasExited)
            {
                try
                {
                    _activeFfmpegProcess.Kill(true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to kill active ffmpeg process during dispose");
                }
                try
                {
                    _activeFfmpegProcess.WaitForExit(100);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error waiting for ffmpeg exit during dispose");
                }
            }
            try
            {
                _activeFfmpegProcess?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispose ffmpeg process");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while disposing ffmpeg process");
        }
        _activeFfmpegProcess = null;

        // Ensure the base libmpv handle is correctly disposed and freed from memory
        try
        {
            base.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while disposing base MpvClient");
        }

        try
        {
            // Stop the mpv worker thread
            _mpvQueue.CompleteAdding();
            try
            {
                _mpvThread?.Join(250);
            }
            catch
            {
                // ignored
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while shutting down mpv worker thread");
        }
    }
}
