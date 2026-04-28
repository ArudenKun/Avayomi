using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Services.Settings;
using Avayomi.Settings;
using HotAvalonia;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.AsyncFile;
using Volo.Abp;
using Volo.Abp.IO;
using Volo.Abp.Modularity.PlugIns;

namespace Avayomi;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var settingsService = SettingsService.Create();
        var loggingLevelSwitch = new LoggingLevelSwitch(
            settingsService.Get<LoggingSettings>().LogEventLevel
        );
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(loggingLevelSwitch)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithDemystifiedStackTraces()
            .WriteTo.AsyncFile(
                AvayomiCoreConsts.Paths.LogsDir.Combine("log.txt"),
                new RollingPolicyOptions { FileSizeLimitBytes = (int)100.Megabytes().Bytes }
            )
            .WriteTo.Async(c => c.Console(outputTemplate: LoggingSettings.Template))
            .CreateLogger();

        var abpApplication = AbpApplicationFactory.Create<AvayomiModule>(options =>
        {
            options.UseAutofac();
            options.Services.AddSingleton(loggingLevelSwitch);
            options.Services.AddLogging(loggingBuilder =>
                loggingBuilder.ClearProviders().AddSerilog(dispose: true)
            );

            var pluginDir = AvayomiCoreConsts.Paths.DataDir.Combine("Plugins");
            DirectoryHelper.CreateIfNotExists(pluginDir);
            options.PlugInSources.AddFolder(pluginDir);
        });

        abpApplication.Initialize();
        var builder = AppBuilder
            .Configure(() => abpApplication.ServiceProvider.GetRequiredService<App>())
            .UsePlatformDetect()
            .UseR3(ex =>
                abpApplication
                    .ServiceProvider.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("R3")
                    .LogError(ex, "An Unhandled R3 Exception occurred")
            )
            .UseHotReload()
            .LogToDelegate(s =>
                abpApplication
                    .ServiceProvider.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Avalonia")
                    .LogWarning("{Log}", s)
            )
            .With(new SkiaOptions { MaxGpuResourceSizeBytes = (long)512.Megabytes().Bytes })
            .WithDeveloperTools()
            .AfterSetup(appBuilder =>
            {
                if (appBuilder.Instance is not { } app)
                    return;

                if (app.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                    return;

                desktop.Exit += (_, _) =>
                {
                    abpApplication.Shutdown();
                    abpApplication.Dispose();
                };
            });

        try
        {
            builder.StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    // ReSharper disable once UnusedMember.Global
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>().UsePlatformDetect().LogToTrace();
}
