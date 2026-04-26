using Avayomi.Mpv.Interop;

namespace Avayomi.Mpv.Player;

public sealed class MpvClientOptions
{
    public MpvClientOptions() { }

    public MpvClientOptions(
        string sharedClientName,
        MpvClient sharedClient,
        bool weakReference = false
    )
    {
        SharedClientName = sharedClientName;
        SharedClient = sharedClient;
        UseWeakReference = weakReference;
    }

    public MpvOpenGlAddressResolver? ResolveOpenGlAddress { get; set; }
    public MpvRenderUpdateCallback? OnRenderInvalidated { get; set; }
    public Action<MpvClient>? BeforeInitialize { get; set; }
    public bool UseWeakReference { get; }
    public MpvClient? SharedClient { get; }
    public string? SharedClientName { get; }
}
