using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avayomi.Mpv.Events;
using Avayomi.Mpv.Native;
using Avayomi.Mpv.Player;
using Serilog;

namespace Avayomi.Controls;

/// <summary>
/// High-level audio player wrapper around libmpv. Exposes playback control,
/// observable properties for UI binding (position, duration, volume, etc.),
/// waveform and spectrum data, and helper methods for loading and managing media.
/// </summary>
public sealed class AudioPlayer : MpvPlayer, INotifyPropertyChanged, IDisposable
{
    private string? _loadedFile;

    private readonly SynchronizationContext? _syncContext;

    private volatile bool _isLoadingMedia,
        _isSeeking;

    private CancellationTokenSource? _seekRestartCts;
    private CancellationTokenSource? _seekDispatchCts;
    private volatile bool _isInternalChange; // Guard to prevent playlist skipping
    private volatile bool _disposed; // Flag to skip native calls during shutdown
    private CancellationTokenSource? _loadCts;
    private int _playbackLoadVersion;

    public static ILogger Log => Serilog.Log.Logger;

    /// <summary>
    /// Holds a reference to the current media item being processed or played.
    /// </summary>
    /// <remarks>This field is null if no media item is currently selected or active.</remarks>
    private MediaItem? _currentMediaItem;

    // private readonly FfMpegSpectrumAnalyzer _spectrumAnalyzer;
    // private readonly FFmpegManager? _ffmpegManager;
    private readonly MpvLibraryManager? _mpvLibraryManager;
    private CancellationTokenSource? _waveformCts;
    private readonly TaskCompletionSource _initTcs = new();

    // Dedicated MPV thread queue and worker to ensure all libmpv interop
    // runs on a single thread that owns the mpv handle.
    private readonly BlockingCollection<Action> _mpvQueue = new();
    private readonly Thread? _mpvThread;
    private int _mpvThreadId;
    private double _volume = 70;

    // Balance: -1 (full left) .. 0 (center) .. 1 (full right)
    private double _balance;

    // Stored equalizer filters string (without balance/pan)
    private string _eqAf = string.Empty;

    // Preamp gain in dB applied via af=volume filter. Positive values allow >100% loudness.
    private double _preampDb = 0.0;

    /// <summary>
    /// Master toggle for applying replay gain / loudness normalization at playback.
    /// </summary>
    private double _replayGainAdjustmentDb;

    // trailing silence computed from waveform (seconds)
    private double _trailingSilenceSeconds;
    private bool _silenceAdvanceFired;

    /// <summary>
    /// Gets or sets a value indicating whether logarithmic volume control is used.
    /// </summary>
    public bool LogarithmicVolumeControl { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether loudness compensation is applied to volume control.
    /// </summary>
    public bool LoudnessCompensatedVolume { get; set; } = true;

    private volatile bool _ignoreTimePos;

    // Track the active ffmpeg process to prevent resource exhaustion on macOS
    private Process? _activeFfmpegProcess;

    // THROTTLING: Keep track of the last time the spectrum was updated
    private long _lastSeekDispatchTicks;

    private static readonly long SeekDispatchThrottleTicks = Stopwatch.Frequency / 15; // ~67 ms between actual seek commands

    /// <summary>
    /// True when a programmatic seek operation is in progress.
    /// </summary>
    public bool IsSeeking => _isSeeking;

    /// <summary>
    /// Gets the media item that is currently selected or being processed, or null if no media item is selected.
    /// </summary>
    /// <remarks>Use this property to access the media item that is currently active in the player. If no
    /// media item is selected, the property returns null. This property is typically used to retrieve information about
    /// the current playback item or to perform actions based on the selected media.</remarks>
    public MediaItem? CurrentMediaItem => _currentMediaItem;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    /// <remarks>This event is typically used in data binding scenarios to notify subscribers that a property
    /// has changed, allowing them to update the UI or perform other actions in response to the change.</remarks>
    public event PropertyChangedEventHandler? PropertyChanged;

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
    private T InvokeOnMpvThread<T>(Func<T> func)
    {
        // If we're already on the mpv thread, invoke directly
        if (Thread.CurrentThread.ManagedThreadId == _mpvThreadId)
            return func();

        // guard against caller using this after the player has been disposed
        if (_mpvQueue.IsAddingCompleted || (_mpvThread != null && !_mpvThread.IsAlive))
            throw new ObjectDisposedException(nameof(AudioPlayer));

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
            return default!;
        }

        return tcs.Task.GetAwaiter().GetResult();
    }

    private void InvokeOnMpvThread(Action action)
    {
        if (Thread.CurrentThread.ManagedThreadId == _mpvThreadId)
        {
            action();
            return;
        }

        if (_mpvQueue.IsAddingCompleted || (_mpvThread != null && !_mpvThread.IsAlive))
            throw new ObjectDisposedException(nameof(AudioPlayer));

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

        if (!tcs.Task.Wait(TimeSpan.FromSeconds(5))) { }
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
        if (Thread.CurrentThread.ManagedThreadId == _mpvThreadId)
        {
            action();
            return;
        }

        if (_mpvQueue.IsAddingCompleted || (_mpvThread != null && !_mpvThread.IsAlive))
            throw new ObjectDisposedException(nameof(AudioPlayer));

        _mpvQueue.Add(action);
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
            catch (Exception ex) { }

            try
            {
                previousLoadCts.Dispose();
            }
            catch (Exception ex) { }
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
        catch (Exception ex) { }

        try
        {
            previousLoadCts.Dispose();
        }
        catch (Exception ex) { }
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
    /// Maximum demuxer cache size in megabytes exposed to the player.
    /// </summary>
    public int CacheSize
    {
        get;
        set
        {
            field = value;
            if (_initTcs.Task.IsCompleted && !_disposed)
                InvokeOnMpvThread(() =>
                {
                    SetProperty("demuxer-max-bytes", $"{value}M");
                    return true;
                });
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
    public double Duration
    {
        get
        {
            // If no file is loaded and player is idle, expose a 1s default instead of 0.
            if (
                (field == 0.0 || double.IsNaN(field))
                && !_disposed
                && !IsPlaying
                && _currentMediaItem == null
            )
                return 0.0;
            return field;
        }
        set
        {
            field = value;
            OnPropertyChanged(nameof(Duration));
            // duration is now available; waveform generation may have been deferred
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
        }
    } = RepeatMode.Off;

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
    /// Creates a new <see cref="AudioPlayer"/> instance and configures
    /// default mpv properties and event handlers.
    /// </summary>
    /// <param name="ffmpegManager">Manager to report activity status for external processes.</param>
    /// <param name="mpvLibraryManager">Manager for libmpv installation signals.</param>
    public AudioPlayer()
    {
        _syncContext = SynchronizationContext.Current;

        _mpvLibraryManager = new MpvLibraryManager();

        if (_mpvLibraryManager != null)
        {
            _mpvLibraryManager.RequestMpvTermination += OnRequestMpvTermination;
            _mpvLibraryManager.InstallationCompleted += OnMpvInstallationCompleted;
        }

        // Always create the analyzer, so it's ready if EnabledSpectrum is toggled

        // Start a dedicated thread that will initialize and own the MPV handle
        // and process all MPV-related actions to avoid native interop crashes.
        _mpvThread = new Thread(() =>
        {
            try
            {
                _mpvThreadId = Thread.CurrentThread.ManagedThreadId;
                // Initialize mpv on this dedicated thread
                InitializeMpvAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error("MPV thread initialization failed", ex);
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
                        Log.Warning("MPV queued action failed", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("MPV queue processing terminated", ex);
            }
        })
        {
            IsBackground = true,
            Name = "mpv-worker",
        };
        _mpvThread.Start();
    }

    /// <summary>
    /// Gets or sets the stereo balance (-1..1). Setting updates the mpv audio filter chain.
    /// </summary>
    public double Balance
    {
        get => _balance;
        set
        {
            if (value < -1)
                value = -1;
            if (value > 1)
                value = 1;
            _balance = value;
            UpdateAf();
            OnPropertyChanged(nameof(Balance));
        }
    }

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
            Log.Error("Failed to initialize AudioPlayer asynchronously", ex);
            _initTcs.TrySetException(ex);
        }
    }

    /// <summary>
    /// Updates the mpv "af" property with the equalizer chain.
    /// Preamp, ReplayGain and Balance are applied via native properties for better stability.
    /// </summary>
    private void UpdateAf()
    {
        try
        {
            if (!(_initTcs.Task.IsCompleted) || _disposed)
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
                    Log.Warning("Failed to set AF equalizer on MPV thread", ex);
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
                Log.Warning("Failed to set balance property on MPV thread", ex);
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
                        Log.Warning(
                            $"Effective volume {effective:0.##}% exceeded max {maxEffectiveVolume}%. Clamping."
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
                Log.Warning("Failed to apply volume/preamp on MPV thread", ex);
            }
        }
        catch (Exception ex)
        {
            Log.Warning("UpdateAf failed", ex);
        }
    }

    private void OnRequestMpvTermination()
    {
        Dispose();
    }

    private async void OnTimeChangedForSilence(object? sender, long ms)
    {
        if (_silenceAdvanceFired || _trailingSilenceSeconds <= 0)
            return;
        // fire early when we enter the silent region
        if (Position >= (Duration - _trailingSilenceSeconds))
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

    private void OnMpvInstallationCompleted(
        object? sender,
        MpvLibraryManager.InstallationCompletedEventArgs e
    )
    {
        if (e.Success && _initTcs.Task.IsFaulted)
        {
            // libmpv was previously missing but now its here, try to re-init
            _ = Task.Run(InitializeMpvAsync);
        }
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
                Log.Warning($"MPV end-file error for '{_loadedFile}': {endData.Error}");
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
                        if (Duration > 0 && Position > (Duration - 1.5))
                        {
                            IsPlaying = false;
                            EndReached?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Loads and starts playback of the specified file path.
    /// </summary>
    public async Task PlayFile(MediaItem item, bool video = false)
    {
        await _initTcs.Task;
        if (_disposed)
            return;
        if (string.IsNullOrEmpty(item.FileName))
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
            item.FileName.Contains("http", StringComparison.OrdinalIgnoreCase)
            && item.OnlineUrls != null
            && item.OnlineUrls.HasValue
        )
        {
            var streamVideoUrl = item.OnlineUrls.Value.Item1;
            var streamAudioUrl = item.OnlineUrls.Value.Item2;

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

        var fileToPlay = !string.IsNullOrWhiteSpace(resolvedUrl) ? resolvedUrl : item.FileName;
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

        // Ensure analyzer is fully stopped and path is updated before loading new file
        InternalStop();

        //Set the current media item
        _currentMediaItem = item;
        OnPropertyChanged(nameof(CurrentMediaItem));

        _syncContext?.Post(
            _ =>
            {
                Position = 0;
            },
            null
        );
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
                                Log.Warning("audio-add failed for external audio stream", audioEx);
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
                Console.WriteLine($"[AudioPlayer Error]: {ex}");
                Debug.WriteLine($"[AudioPlayer Error]: {ex}");
                Log.Error("loadfile error", ex);

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

    private CancellationTokenSource? _eqCts;

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

                if (!_disposed)
                {
                    // update the analyzer start position and force a restart if it
                    // is currently running.  this handles the common case where the
                    // analyzer is active while the user seeks; without restarting the
                    // internal ffmpeg process it will continue decoding from the old
                    // offset and the spectrum will remain stuck until something else
                    // (eg. pause/play) restarts it.
                }

                // clear the seeking flag before attempting to start the analyzer so
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
        _waveformCts?.Cancel();

        try
        {
            if (_activeFfmpegProcess != null && !_activeFfmpegProcess.HasExited)
                _activeFfmpegProcess.Kill(true);
        }
        catch (Exception ex)
        {
            Log.Warning("Error killing active ffmpeg process during SuspendForEditing", ex);
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
        PostToMpvThread(() => _ = RunCommandAsync(new[] { "loadfile", ToMpvLoadTarget(path) }));

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
            PostToMpvThread(() => _ = RunCommandAsync(new[] { "stop" }));
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
        _currentMediaItem = null;
        OnPropertyChanged(nameof(CurrentMediaItem));
    }

    private void InternalStop()
    {
        IsPlaying = false;
        Position = 0;
        ClearMedia();
        Stopped?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Writes the provided bytes to a temporary file and begins playback.
    /// </summary>
    /// <param name="b">Byte buffer containing media data.</param>
    /// <param name="m">MIME type hint (unused).</param>
    public async Task PlayBytes(byte[]? b, string m = "video/mp4")
    {
        await _initTcs.Task;
        if (b == null)
            return;
        var p = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp");
        await File.WriteAllBytesAsync(p, b);
        await PlayFile(new MediaItem() { FileName = p });
    }

    /// <summary>
    /// Dispose managed resources used by the player (stops analyzers and
    /// kills any active FFmpeg helper process).
    /// </summary>
    public new void Dispose()
    {
        CancelPendingPlaybackLoad();
        if (_disposed)
            return;
        _disposed = true;

        if (_mpvLibraryManager != null)
        {
            _mpvLibraryManager.RequestMpvTermination -= OnRequestMpvTermination;
            _mpvLibraryManager.InstallationCompleted -= OnMpvInstallationCompleted;
        }

        try
        {
            EventReceived -= OnMpvEvent;
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to remove mpv event handler", ex);
        }

        try { }
        catch (Exception ex)
        {
            Log.Warning("Failed to stop spectrum analyzer", ex);
        }

        try { }
        catch (Exception ex)
        {
            Log.Warning("Failed to clear spectrum analyzer path", ex);
        }

        try
        {
            _waveformCts?.Cancel();
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to cancel waveform CTS during dispose", ex);
        }

        try
        {
            _waveformCts?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to dispose waveform CTS during dispose", ex);
        }

        _waveformCts = null;

        try
        {
            _eqCts?.Cancel();
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to cancel eq CTS during dispose", ex);
        }

        try
        {
            _eqCts?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to dispose eq CTS during dispose", ex);
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
                    Log.Warning("Failed to kill active ffmpeg process during dispose", ex);
                }

                try
                {
                    _activeFfmpegProcess.WaitForExit(100);
                }
                catch (Exception ex)
                {
                    Log.Warning("Error waiting for ffmpeg exit during dispose", ex);
                }
            }

            try
            {
                _activeFfmpegProcess?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to dispose ffmpeg process", ex);
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Error while disposing ffmpeg process", ex);
        }

        _activeFfmpegProcess = null;

        // Ensure the base libmpv handle is correctly disposed and freed from memory
        try
        {
            base.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warning("Error while disposing base AesMpvPlayer", ex);
        }

        try
        {
            // Stop the mpv worker thread
            _mpvQueue.CompleteAdding();
            try
            {
                _mpvThread?.Join(250);
            }
            catch { }
        }
        catch (Exception ex)
        {
            Log.Warning("Error while shutting down mpv worker thread", ex);
        }
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
}
