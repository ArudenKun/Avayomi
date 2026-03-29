using Avalonia.Controls;

namespace Avayomi.Hosting;

public sealed class AvayomiHostingOptions
{
    public ShutdownMode ShutdownMode { get; set; } = ShutdownMode.OnMainWindowClose;
}
