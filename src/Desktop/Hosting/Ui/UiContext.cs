using Avalonia;
using Avalonia.Threading;

namespace Desktop.Hosting.Ui;

/// <summary>
/// Encapsulates the information needed to manage the hosting of a WinUI based
/// User Interface service and associated thread.
/// </summary>
public class UiContext : BaseUiContext
{
    /// <summary>Gets or sets the Avalonia dispatcher.</summary>
    /// <value>The Avalonia dispatcher.</value>
    public Dispatcher? Dispatcher { get; set; }

    /// <summary>Gets or sets the Avalonia Application instance.</summary>
    /// <value>The Avalonia Application instance.</value>
    public Application? Application { get; set; }
}
