using System;
using System.Threading.Tasks;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Desktop.Hosting;

public sealed class AvayomiApp : IDisposable
{
    private readonly ILogger<AvayomiApp> _logger;
    private readonly IHostedServiceManager _hostedServiceManager;
    private readonly AvayomiAppOptions _avayomiAppOptions;


    public AvayomiApp(IServiceProvider services)
    {
        Services = services;
        _logger = services.GetService<ILogger<AvayomiApp>>() ?? NullLogger<AvayomiApp>.Instance;
        _hostedServiceManager = services.GetRequiredService<IHostedServiceManager>();
        _avayomiAppOptions = services.GetRequiredService<AvayomiAppOptions>();
    }

    public IServiceProvider Services { get; }

    public static AvayomiAppBuilder CreateBuilder(string[] args)
        => new(args);

    public void Run()
    {
        RunAsync().GetAwaiter().GetResult();
    }

    public async Task RunAsync()
    {
        var appBuilder = AppBuilder.Configure(() => Services.GetRequiredService<Application>())
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
        _avayomiAppOptions.ConfigureAppBuilderDelegate?.Invoke(appBuilder);

        _logger.LogInformation("Staring Application");
        var startTask = _hostedServiceManager.StartAllAsync();
        _logger.LogInformation("Application Started");
        appBuilder.StartWithClassicDesktopLifetime(_avayomiAppOptions.Args);
        _logger.LogInformation("Stopping Application");
        var stopTask = _hostedServiceManager.StopAllAsync();
        await startTask;
        await stopTask;
        _logger.LogInformation("Application Stopped");
    }

    public void Dispose() => _hostedServiceManager.Dispose();
}