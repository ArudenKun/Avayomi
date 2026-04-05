using Avayomi.Mpv.Interop;

namespace Avayomi.Mpv.Player;

public sealed class MpvPlayerOptions
{
    public MpvPlayerOptions() { }

    public MpvPlayerOptions(
        string sharedClientName,
        MpvPlayer sharedPlayer,
        bool weakReference = false
    )
    {
        SharedClientName = sharedClientName;
        SharedPlayer = sharedPlayer;
        UseWeakReference = weakReference;
    }

    public MpvOpenGlAddressResolver? ResolveOpenGlAddress { get; set; }
    public MpvRenderUpdateCallback? OnRenderInvalidated { get; set; }
    public Action<MpvPlayer>? BeforeInitialize { get; set; }
    public bool UseWeakReference { get; }
    public MpvPlayer? SharedPlayer { get; }
    public string? SharedClientName { get; }
}
