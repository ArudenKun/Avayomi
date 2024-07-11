using System.Threading.Tasks;

namespace Desktop.Hosting.Ui;

/// <summary>
///     Represents a Ui Thread in a hosted application.
/// </summary>
public interface IUiThread
{
    /// <summary>Starts the User Interface thread.</summary>
    /// <remarks>
    ///     Note that after calling this method, the thread may not be actually
    ///     running. To check if that is the case or not use the <see cref="BaseUiContext.IsRunning" />.
    /// </remarks>
    void StartUiThread();

    /// <summary>
    ///     Asynchronously request the User Interface thread to stop.
    /// </summary>
    /// <returns>
    ///     The asynchronous task on which to wait for completion.
    /// </returns>
    Task StopUiThreadAsync();
}
