using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avayomi.Mpv.Player;

namespace Avayomi.Controls;

public enum VideoFlip
{
    None,
    Horizontal,
    Vertical,
    Both,
}

public class MediaPlayerControl : OpenGlControlBase
{
    private const double DefaultHeartbeatFps = 60.0;
    private static readonly TimeSpan ViewportExpansionDelay = TimeSpan.FromSeconds(2);

    private bool _initialized;
    private bool _hasRenderedOnceSincePause;
    private GlInterface? _glInterface;
    private double _videoAspectRatio;
    private long _viewportExpansionHoldUntilTicks;

    public static readonly StyledProperty<MpvClient?> PlayerProperty = AvaloniaProperty.Register<
        MediaPlayerControl,
        MpvClient?
    >(nameof(Client));

    public MpvClient? Client
    {
        get => GetValue(PlayerProperty);
        set => SetValue(PlayerProperty, value);
    }

    public static readonly StyledProperty<bool> IsRenderingPausedProperty =
        AvaloniaProperty.Register<MediaPlayerControl, bool>(nameof(IsRenderingPaused));

    public bool IsRenderingPaused
    {
        get => GetValue(IsRenderingPausedProperty);
        set => SetValue(IsRenderingPausedProperty, value);
    }

    public static readonly StyledProperty<Stretch> StretchProperty = AvaloniaProperty.Register<
        MediaPlayerControl,
        Stretch
    >(nameof(Stretch), Stretch.Uniform);

    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public static readonly StyledProperty<int> RotationProperty = AvaloniaProperty.Register<
        MediaPlayerControl,
        int
    >(nameof(Rotation));

    public int Rotation
    {
        get => GetValue(RotationProperty);
        set => SetValue(RotationProperty, value);
    }

    public static readonly StyledProperty<VideoFlip> FlipProperty = AvaloniaProperty.Register<
        MediaPlayerControl,
        VideoFlip
    >(nameof(Flip));

    public VideoFlip Flip
    {
        get => GetValue(FlipProperty);
        set => SetValue(FlipProperty, value);
    }

    public static readonly StyledProperty<double> HeartbeatFpsProperty = AvaloniaProperty.Register<
        MediaPlayerControl,
        double
    >(nameof(HeartbeatFps), DefaultHeartbeatFps);

    public double HeartbeatFps
    {
        get => GetValue(HeartbeatFpsProperty);
        set => SetValue(HeartbeatFpsProperty, value);
    }

    public static readonly StyledProperty<bool> UseCustomHeartbeatProperty =
        AvaloniaProperty.Register<MediaPlayerControl, bool>(nameof(UseCustomHeartbeat));

    public bool UseCustomHeartbeat
    {
        get => GetValue(UseCustomHeartbeatProperty);
        set => SetValue(UseCustomHeartbeatProperty, value);
    }

    public static readonly StyledProperty<double> AudioSyncOffsetProperty =
        AvaloniaProperty.Register<MediaPlayerControl, double>(nameof(AudioSyncOffset));

    public double AudioSyncOffset
    {
        get => GetValue(AudioSyncOffsetProperty);
        set => SetValue(AudioSyncOffsetProperty, value);
    }

    public static readonly DirectProperty<
        MediaPlayerControl,
        double
    > ExpandedViewportWidthProperty = AvaloniaProperty.RegisterDirect<MediaPlayerControl, double>(
        nameof(ExpandedViewportWidth),
        o => o.ExpandedViewportWidth
    );

    public static readonly StyledProperty<double> ReferenceViewportWidthProperty =
        AvaloniaProperty.Register<MediaPlayerControl, double>(nameof(ReferenceViewportWidth));

    public double ReferenceViewportWidth
    {
        get => GetValue(ReferenceViewportWidthProperty);
        set => SetValue(ReferenceViewportWidthProperty, value);
    }

    public static readonly StyledProperty<double> ReferenceViewportHeightProperty =
        AvaloniaProperty.Register<MediaPlayerControl, double>(nameof(ReferenceViewportHeight));

    public double ReferenceViewportHeight
    {
        get => GetValue(ReferenceViewportHeightProperty);
        set => SetValue(ReferenceViewportHeightProperty, value);
    }

    public double ExpandedViewportWidth
    {
        get;
        private set => SetAndRaise(ExpandedViewportWidthProperty, ref field, value);
    }

    private static double GetEffectiveHeartbeatFps(double heartbeatFps) =>
        heartbeatFps > 0 ? heartbeatFps : DefaultHeartbeatFps;

    private void ResetViewportSizing()
    {
        _videoAspectRatio = 0;
        _viewportExpansionHoldUntilTicks = 0;
        UpdateExpandedViewportWidth();
    }

    private void HoldViewportExpansion()
    {
        _viewportExpansionHoldUntilTicks =
            Stopwatch.GetTimestamp()
            + (long)(ViewportExpansionDelay.TotalSeconds * Stopwatch.Frequency);
        UpdateExpandedViewportWidth();
    }

    private void UpdateExpandedViewportWidth()
    {
        double baseWidth = ReferenceViewportWidth > 0 ? ReferenceViewportWidth : Bounds.Width;
        double baseHeight = ReferenceViewportHeight > 0 ? ReferenceViewportHeight : Bounds.Height;

        double targetWidth =
            baseWidth > 0
                ? Math.Round(baseWidth, 2)
                : (Bounds.Width > 0 ? Math.Round(Bounds.Width, 2) : 0);

        if (
            Stopwatch.GetTimestamp() >= _viewportExpansionHoldUntilTicks
            && _videoAspectRatio > 0
            && baseHeight > 0
        )
        {
            double videoWidth = Math.Round(baseHeight * _videoAspectRatio, 2);
            targetWidth = Math.Max(targetWidth, videoWidth);
        }

        ExpandedViewportWidth = targetWidth;
    }

    private void RefreshVideoAspectRatio()
    {
        if (Client == null)
            return;

        double aspect;

        try
        {
            aspect = Client.GetDoubleProperty("video-out-params/aspect");
        }
        catch
        {
            try
            {
                aspect = Client.GetDoubleProperty("video-params/aspect");
            }
            catch
            {
                return;
            }
        }

        if (aspect <= 0 || double.IsNaN(aspect) || double.IsInfinity(aspect))
            return;

        if (Math.Abs(aspect - _videoAspectRatio) < 0.01)
            return;

        _videoAspectRatio = aspect;
        UpdateExpandedViewportWidth();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _hasRenderedOnceSincePause = false;
        ResetViewportSizing();
        HoldViewportExpansion();
        if (!IsRenderingPaused)
            RequestNextFrameRendering();
    }

    private IntPtr GetProcAddressInternal(IntPtr ctx, string name) =>
        _glInterface?.GetProcAddress(name) ?? IntPtr.Zero;

    protected override void OnOpenGlInit(GlInterface gl)
    {
        _glInterface = gl;
        _initialized = false;
        _hasRenderedOnceSincePause = false;
        ResetViewportSizing();
        HoldViewportExpansion();
    }

    private void InitializeMpvInternal()
    {
        if (Client == null || _glInterface == null)
            return;

        try
        {
            Client.Options.ResolveOpenGlAddress = GetProcAddressInternal;
            double heartbeatFps = GetEffectiveHeartbeatFps(HeartbeatFps);

            Client.SetProperty("video-sync", UseCustomHeartbeat ? "display-resample" : "audio");
            Client.SetProperty("audio-pitch-correction", "yes");
            Client.SetProperty("hwdec", "auto-safe");
            Client.SetProperty("opengl-waitvsync", "no");
            Client.SetProperty("override-display-fps", "0");

            if (UseCustomHeartbeat)
            {
                Client.SetProperty(
                    "override-display-fps",
                    heartbeatFps.ToString(CultureInfo.InvariantCulture)
                );
                Client.SetProperty("interpolation", "yes");
                Client.SetProperty("tscale", "oversample");
            }
            else
            {
                Client.SetProperty("interpolation", "no");
                Client.SetProperty("override-display-fps", "0");
            }

            Client.EnsureRenderContext();

            ApplyStretch(Stretch);
            ApplyRotation(Rotation);
            ApplyFlip(Flip);
            ApplyAudioOffset(AudioSyncOffset);

            _initialized = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"VideoViewControl Init Error: {ex.Message}");
        }
    }

    private void ApplyStretch(Stretch stretch)
    {
        if (Client == null)
            return;
        switch (stretch)
        {
            case Stretch.None:
                Client.SetProperty("video-unscaled", "yes");
                Client.SetProperty("panscan", "0");
                break;
            case Stretch.Fill:
                Client.SetProperty("video-unscaled", "no");
                Client.SetProperty("keepaspect", "no");
                Client.SetProperty("panscan", "0");
                break;
            case Stretch.Uniform:
                Client.SetProperty("video-unscaled", "no");
                Client.SetProperty("keepaspect", "yes");
                Client.SetProperty("panscan", "0");
                break;
            case Stretch.UniformToFill:
                Client.SetProperty("video-unscaled", "no");
                Client.SetProperty("keepaspect", "yes");
                Client.SetProperty("panscan", "1.0");
                break;
        }
    }

    private void ApplyRotation(int degrees) =>
        Client?.SetProperty("video-rotate", degrees.ToString(CultureInfo.InvariantCulture));

    private void ApplyFlip(VideoFlip flip)
    {
        if (Client == null)
            return;
        string filter = flip switch
        {
            VideoFlip.Horizontal => "hflip",
            VideoFlip.Vertical => "vflip",
            VideoFlip.Both => "hflip,vflip",
            _ => "",
        };
        Client.SetProperty("vf", filter);
    }

    private void ApplyAudioOffset(double ms)
    {
        double seconds = ms / 1000.0;
        Client?.SetProperty("audio-delay", seconds.ToString(CultureInfo.InvariantCulture));
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (IsRenderingPaused && _hasRenderedOnceSincePause)
            return;

        if (!_initialized)
            InitializeMpvInternal();
        if (Client == null || !_initialized)
            return;

        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var width = (int)(Bounds.Width * scale);
        var height = (int)(Bounds.Height * scale);

        if (width > 0 && height > 0)
        {
            gl.BindFramebuffer(0x8D40, fb);
            gl.Viewport(0, 0, width, height);
            Client.RenderToOpenGl(width, height, fb, flipY: 1);
            RefreshVideoAspectRatio();
            UpdateExpandedViewportWidth();

            if (IsRenderingPaused)
                _hasRenderedOnceSincePause = true;
        }

        if (!IsRenderingPaused && IsVisible)
            RequestNextFrameRendering();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsVisibleProperty)
        {
            if (change.GetNewValue<bool>())
            {
                _hasRenderedOnceSincePause = false;
                ResetViewportSizing();
                HoldViewportExpansion();
                if (!IsRenderingPaused)
                    RequestNextFrameRendering();
            }
            else
            {
                ResetViewportSizing();
            }
        }
        else if (change.Property == IsRenderingPausedProperty)
        {
            bool paused = change.GetNewValue<bool>();
            if (paused)
            {
                _hasRenderedOnceSincePause = false;
                ResetViewportSizing();
                RequestNextFrameRendering();
            }
            else
            {
                _hasRenderedOnceSincePause = false;
                ResetViewportSizing();
                HoldViewportExpansion();
                RequestNextFrameRendering();
            }
        }
        else if (change.Property == StretchProperty)
        {
            ApplyStretch(change.GetNewValue<Stretch>());
            RequestNextFrameRendering();
        }
        else if (change.Property == RotationProperty)
        {
            ApplyRotation(change.GetNewValue<int>());
            RequestNextFrameRendering();
        }
        else if (change.Property == FlipProperty)
        {
            ApplyFlip(change.GetNewValue<VideoFlip>());
            RequestNextFrameRendering();
        }
        else if (change.Property == AudioSyncOffsetProperty)
            ApplyAudioOffset(change.GetNewValue<double>());
        else if (change.Property == BoundsProperty)
        {
            UpdateExpandedViewportWidth();
            RequestNextFrameRendering();
        }
        else if (
            change.Property == ReferenceViewportWidthProperty
            || change.Property == ReferenceViewportHeightProperty
        )
        {
            ResetViewportSizing();
            HoldViewportExpansion();
            RequestNextFrameRendering();
        }
        else if (change.Property == UseCustomHeartbeatProperty || change.Property == PlayerProperty)
        {
            _initialized = false;
            _hasRenderedOnceSincePause = false;
            ResetViewportSizing();
            HoldViewportExpansion();
            RequestNextFrameRendering();
        }
        else if (change.Property == HeartbeatFpsProperty)
        {
            double heartbeatFps = GetEffectiveHeartbeatFps(change.GetNewValue<double>());
            if (UseCustomHeartbeat)
                Client?.SetProperty(
                    "override-display-fps",
                    heartbeatFps.ToString(CultureInfo.InvariantCulture)
                );
        }
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _initialized = false;
        _hasRenderedOnceSincePause = false;
        _glInterface = null;
        ResetViewportSizing();
        base.OnOpenGlDeinit(gl);
    }
}
