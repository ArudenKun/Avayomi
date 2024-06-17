using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Avayomi.Hosting.Ui;

/// <summary>
/// A long running service that will execute the User Interface
/// thread.
/// </summary>
/// <remarks>
/// <para>
/// Should be registered (only once) in the services collection with the
/// <see cref="ServiceCollectionHostedServiceExtensions.AddHostedService{THostedService}(IServiceCollection)">
/// AddHostedService
/// </see>
/// extension method.
/// </para>
/// <para>
/// Expects the <see cref="UiThread" /> and <see cref="UiContext" />
/// singleton instances to be setup in the dependency injector.
/// </para>
/// </remarks>
public sealed partial class UiThreadHostedService : IHostedService
{
    private readonly IUiThread _uiThread;
    private readonly UiContext _uiContext;
    private readonly ILogger<UiThreadHostedService> _logger;


    public UiThreadHostedService(IUiThread uiThread, UiContext uiContext, ILogger<UiThreadHostedService>? logger)
    {
        _uiThread = uiThread;
        _uiContext = uiContext;
        _logger = logger ?? NullLogger<UiThreadHostedService>.Instance;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        // Make the UI thread go
        _uiThread.StartUiThread();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || !_uiContext.IsRunning)
        {
            return Task.CompletedTask;
        }

        StoppingUiThread();
        return _uiThread.StopUiThreadAsync();
    }

    [LoggerMessage(
        SkipEnabledCheck = true,
        Level = LogLevel.Debug,
        Message = "Stopping UiThread due to application exiting."
    )]
    partial void StoppingUiThread();
}