using System;
using AsyncImageLoader;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Logging;
using Avayomi.Services;
using Avayomi.Services.Settings;
using Avayomi.Settings;
using Avayomi.Utilities;
using Avayomi.Views;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PleasantUI;
using Serilog;
using Serilog.Core;
using Serilog.Events;
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
        var loggingSwitch = new LoggingLevelSwitch(
            settingsService.Get<LoggingSettings>().LogEventLevel
        );
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(loggingSwitch)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithDemystifiedStackTraces()
            .WriteTo.Async(c =>
                c.File(
                    AvayomiCoreConsts.Paths.LogsDir.Combine("log.txt"),
                    outputTemplate: LoggingSettings.Template,
                    retainedFileTimeLimit: 30.Days(),
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    shared: true
                )
            )
            .WriteTo.Async(c => c.Console(outputTemplate: LoggingSettings.Template))
            .CreateLogger();

        var abpApplication = AbpApplicationFactory.Create<AvayomiModule>(options =>
        {
            options.UseAutofac();
            options.Services.AddLogging(loggingBuilder =>
                loggingBuilder.ClearProviders().AddSerilog(dispose: true)
            );
            options.Services.AddSingleton(loggingSwitch);
            var pluginDir = AvayomiCoreConsts.Paths.DataDir.Combine("Plugins");
            DirectoryHelper.CreateIfNotExists(pluginDir);
            options.PlugInSources.AddFolder(pluginDir);
        });

        var lifetime = new ClassicDesktopStyleApplicationLifetime
        {
            Args = args,
            ShutdownMode = ShutdownMode.OnMainWindowClose,
        };

        abpApplication.Initialize();

        AppBuilder
            .Configure<Application>()
            .UsePlatformDetect()
            .UseR3(ex =>
                abpApplication
                    .ServiceProvider.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("R3")
                    .LogError(ex, "An Unhandled R3 Exception occurred")
            )
            .UseViewInitializationStrategy(ViewInitializationStrategy.Lazy)
            .UseComponentControlFactory(type =>
                (Control)
                    ActivatorUtilities.GetServiceOrCreateInstance(
                        abpApplication.ServiceProvider,
                        type
                    )
            )
            .UseServiceProvider(abpApplication.ServiceProvider)
            .UseHotReload()
            .LogToSerilog()
            .WithDeveloperTools()
            .With(new SkiaOptions { MaxGpuResourceSizeBytes = (long)512.Megabytes().Bytes })
            .AfterSetup(appBuilder =>
            {
                abpApplication.ServiceProvider.GetRequiredService<ILoggingService>().Initialize();

                var replicantImageLoader = new ReplicantImageLoader(
                    AvayomiCoreConsts.Paths.CacheDir.Combine("Images")
                );
                ImageLoader.AsyncImageLoader = replicantImageLoader;
                ImageBrushLoader.AsyncImageLoader = replicantImageLoader;

                if (appBuilder.Instance is not { } application)
                    return;

                application.Styles.Add(new PleasantTheme());
            })
            .SetupWithLifetime(lifetime);

        lifetime.Exit += (_, _) =>
        {
            abpApplication.Shutdown();
            abpApplication.Dispose();
        };

        lifetime.MainWindow = abpApplication.ServiceProvider.GetRequiredService<MainWindow>();

        try
        {
            lifetime.Start(args);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
