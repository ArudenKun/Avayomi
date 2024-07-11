using System;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Avalonia;
using CommunityToolkit.Mvvm.DependencyInjection;
using Desktop.Extensions;
using Desktop.Helpers;
using Desktop.Hosting;
using Desktop.Models;
using Desktop.Services;
using Desktop.Services.Caching;
using Desktop.Services.Settings;
using Generator.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers.ClassName;
using Serilog.Events;
using Serilog.Sinks.FileEx;
using Velopack;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization;

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
        builder.ConfigureAvalonia<App>();
        builder
            .Services.AddSingleton<IJsonTypeInfoResolver>(GlobalJsonContext.Default)
            .AddSingleton(
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    TypeInfoResolver = GlobalJsonContext.Default
                }
            )
            .AddSingleton(
                new FileCacheOptions(EnvironmentHelper.ApplicationDataPath.JoinPath("cache"))
            )
            .AddSingleton<ISettingsProvider<AppSettings>, AppSettingsProvider>()
            .AddSingleton<IDistributedCache, FileCache>()
            .AddSingleton<IFusionCacheSerializer, FileCacheSerializer>()
            .AddSingleton<IFileSystem, FileSystem>()
            .AddSingleton<IFileSystemService, FileSystemService>()
            .AddCore()
            .AddSerilog(
                (sp, loggerConfig) =>
                {
                    const string logTemplate =
                        "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3} {ClassName}] {Message:lj} {NewLine}{Exception}";
                    loggerConfig
                        .MinimumLevel.Is(
                            IsDebug() ? LogEventLevel.Debug : LogEventLevel.Information
                        )
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
                        .Enrich.WithClassName();
                }
            )
            .AddFusionCache()
            .WithDefaultEntryOptions(opt =>
                opt.SetDuration(TimeSpan.FromMinutes(5))
                    .SetFailSafe(true)
                    .SetFactoryTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30))
            )
            .TryWithAutoSetup();

        using var app = builder.Build();

        // Ioc.Default.ConfigureServices(app.Services);

        try
        {
            VelopackApp.Build().Run(app.Services.GetRequiredService<ILogger<VelopackApp>>());
            app.Run();
        }
        catch (Exception e)
        {
            app.Services.GetRequiredService<ILogger<AvayomiApp>>().LogException(e);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    // ReSharper disable once UnusedMember.Global
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<Application>().UsePlatformDetect().WithInterFont().LogToTrace();
    }

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
