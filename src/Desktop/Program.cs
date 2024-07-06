using System;
using Avalonia;
using Core.Helpers;
using Desktop.Extensions;
using Desktop.Hosting;
using Generator.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers.ClassName;
using Serilog.Events;
using Serilog.Sinks.FileEx;
using Velopack;

namespace Desktop;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        ConfigureLogging();
        var builder = AvayomiApp.CreateBuilder(args);
        builder.ConfigureAvayomiApp<App>();

        builder.Services.AddCore();

        builder.Logging.ClearProviders().AddSerilog();

        using var app = builder.Build();

        try
        {
            VelopackApp.Build().Run(app.Services.GetRequiredService<ILogger<VelopackApp>>());
            app.Run();
        }
        catch (Exception e)
        {
            app.Services.GetRequiredService<ILogger<AvayomiApp>>().LogException(e);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    // ReSharper disable once UnusedMember.Global
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>().UsePlatformDetect().WithInterFont().LogToTrace();

    private static void ConfigureLogging()
    {
        const string logTemplate =
            "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3} {ClassName}] {Message:lj} {NewLine}{Exception}";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(IsDebug() ? LogEventLevel.Debug : LogEventLevel.Information)
            .WriteTo.Console(outputTemplate: logTemplate)
            .WriteTo.Async(x =>
                x.FileEx(
                    EnvironmentHelper.ApplicationDataPath.JoinPath(
                        "logs",
                        $"logs{(IsDebug() ? ".debug" : "")}.txt"
                    ),
                    ".dd-MM-yyyy",
                    outputTemplate: logTemplate,
                    rollingInterval: RollingInterval.Day,
                    rollOnEachProcessRun: false,
                    rollOnFileSizeLimit: true,
                    preserveLogFileName: true,
                    shared: true
                )
            )
            .Enrich.FromLogContext()
            .Enrich.WithClassName()
            .CreateLogger();
    }

    private static bool IsDebug()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}
