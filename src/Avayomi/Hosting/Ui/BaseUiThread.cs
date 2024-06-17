using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Avayomi.Hosting.Ui;

/// <summary>
/// Represents a base class for a user interface thread in a hosted
/// application.
/// </summary>
/// <typeparam name="T">
/// The concrete type of the class extending <see cref="BaseUiContext" />
/// which will provide the necessary options to setup the User Interface.
/// </typeparam>
public abstract class BaseUiThread<T> : IDisposable, IUiThread where T : BaseUiContext
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    private readonly ManualResetEvent _serviceManualResetEvent = new(false);

    protected BaseUiThread(IHostApplicationLifetime hostApplicationLifetime, T uiContext, ILogger? logger)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        UiContext = uiContext;
        Logger = logger ?? NullLogger.Instance;
        
        // Create a thread which runs the UI
        var newUiThread = new Thread(() =>
        {
            _ = _serviceManualResetEvent.WaitOne(); // wait for the signal to actually start
            UiContext.IsRunning = true;
            UiThreadStart();
            UiThreadExit();
        })
        {
            IsBackground = true,
        };

        if (OperatingSystem.IsWindows())
        {
            // Set the apartment state
            newUiThread.SetApartmentState(ApartmentState.STA);
        }

        // Transition the new UI thread to the RUNNING state. Note that the
        // thread will actually start after the `serviceManualResetEvent` is
        // set.
        newUiThread.Start();
    }

    /// <summary>
    /// Gets the hosting context for the user interface service.
    /// </summary>
    /// <value>
    /// Although never <c>null</c>, the different fields of the hosting context
    /// may or may not contain valid values depending on the current state of
    /// the User Interface thread. Refer to the concrete class documentation.
    /// </value>
    protected T UiContext { get; }

    protected ILogger Logger { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _serviceManualResetEvent.Dispose();
    }

    /// <summary>
    /// Do the work needed to actually start the User Interface thread.
    /// </summary>
    protected abstract void UiThreadStart();

    public void StartUiThread() => _serviceManualResetEvent.Set();

    /// <inheritdoc />
    public abstract Task StopUiThreadAsync();

    private void UiThreadExit()
    {
        Debug.Assert(
            UiContext.IsRunning,
            "Expecting the `IsRunning` flag to be set when `UiThreadExit() is called"
        );
        UiContext.IsRunning = false;
        if (!UiContext.IsLifetimeLinked) return;

        Logger.LogDebug("Stopping hosted application due to UiThread exit.");
        if (
            !_hostApplicationLifetime.ApplicationStopped.IsCancellationRequested
            && !_hostApplicationLifetime.ApplicationStopping.IsCancellationRequested
        )
        {
            _hostApplicationLifetime.StopApplication();
        }
    }
}