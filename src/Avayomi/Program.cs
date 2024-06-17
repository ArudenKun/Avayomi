using Avalonia;
using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Avayomi.Core;
using Avayomi.Data.Caching;
using Avayomi.Helpers;
using Avayomi.Hosting;
using Avayomi.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers.ClassName;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.FileEx;
using Velopack;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Avayomi;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        SetupLogger();
        try
        {
            VelopackApp.Build().Run(new SerilogLoggerFactory().CreateLogger<VelopackApp>());
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddSingleton<Settings>()
                .AddSingleton<IJsonTypeInfoResolver>(GlobalJsonContext.Default)
                .AddSingleton(
                    new JsonSerializerOptions
                    {
                        TypeInfoResolver = GlobalJsonContext.Default,
                        WriteIndented = true
                    }
                )
                .AddSingleton(
                    new FileCacheOptions(EnvironmentHelper.ApplicationDataPath.JoinPath("cache"))
                )
                .AddSingleton<IFusionCacheSerializer, FusionCacheSystemTextJsonSerializer>()
                .AddSingleton<IFileCache, FileCache>()
                .AddSingleton<IDistributedCache>(sp => sp.GetRequiredService<IFileCache>());

            builder
                .Services.AddCore()
                .AddSerilog()
                .AddFusionCache()
                .WithDefaultEntryOptions(opt =>
                    opt.SetDuration(TimeSpan.FromMinutes(5))
                        .SetFailSafe(true)
                        .SetFactoryTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30))
                )
                .WithRegisteredSerializer()
                .WithRegisteredDistributedCache();

            builder.Logging.ClearProviders().AddSerilog(dispose: true);

            using var host = builder.ConfigureAvalonia<App>().Build();

            Ioc.Default.ConfigureServices(host.Services);

            host.Run();
        }
        catch (Exception e)
        {
            Log.Error(e, "An error occured");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void SetupLogger()
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

    // Avalonia configuration, don't remove; also used by visual designer.
    // ReSharper disable once UnusedMember.Global
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<Application>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}