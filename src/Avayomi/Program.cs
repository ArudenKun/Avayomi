using System;
using Autofac;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Hosting;
using Avayomi.Services;
using Avayomi.Settings;
using Avayomi.ViewModels;
using HotAvalonia;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Volo.Abp.Autofac;
using Volo.Abp.DependencyInjection;
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
        var builder = Host.CreateApplicationBuilder(args);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithDemystifiedStackTraces()
            .WriteTo.Async(c => c.Console(outputTemplate: LoggingSettings.Template))
            .CreateBootstrapLogger();

        builder.AddAutofac();
        builder.AddAvaloniaHosting<App>(
            (sp, appBuilder) =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                appBuilder
                    .UsePlatformDetect()
                    .UseR3(ex =>
                        loggerFactory
                            .CreateLogger("R3")
                            .LogError(ex, "An Unhandled R3 Exception occurred")
                    )
                    .UseHotReload()
                    .LogToDelegate(s =>
                        loggerFactory.CreateLogger("Avalonia").LogWarning("{Log}", s)
                    )
                    .With(new SkiaOptions { MaxGpuResourceSizeBytes = (long)512.Megabytes().Bytes })
                    .AfterSetup(_ =>
                    {
                        if (appBuilder.Instance is not { } app)
                            return;

                        if (
                            app.ApplicationLifetime
                            is not IClassicDesktopStyleApplicationLifetime desktop
                        )
                            return;

                        sp.GetRequiredService<IThemeService>().Initialize();

                        var mainWindow =
                            (Window?)
                                app.DataTemplates[0]
                                    .Build(sp.GetRequiredService<MainWindowViewModel>())
                            ?? throw new InvalidOperationException("Could not find Main Window");
                        TopLevel topLevel = desktop.MainWindow = mainWindow;
                        sp.GetRequiredService<ObjectAccessor<TopLevel>>().Value = topLevel;
                    });
            }
        );
        builder.Services.AddSingleton<LoggingLevelSwitch>();
        builder.Services.AddSerilog(
            (sp, loggerConfiguration) =>
                loggerConfiguration
                    .MinimumLevel.ControlledBy(sp.GetRequiredService<LoggingLevelSwitch>())
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
        );

        var abpApplication = builder.Services.AddApplication<AvayomiModule>(options =>
        {
            var pluginDir = AvayomiCoreConsts.Paths.DataDir.Combine("Plugins");
            DirectoryHelper.CreateIfNotExists(pluginDir);
            options.PlugInSources.AddFolder(pluginDir);
        });

        var app = builder.Build();
        abpApplication.Initialize(app.Services);
        app.Services.GetRequiredService<ILoggingService>().Initialize();
        app.Run();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();

    // ReSharper disable once UnusedMethodReturnValue.Local
    private static IHostApplicationBuilder AddAutofac(
        this IHostApplicationBuilder builder,
        Action<ContainerBuilder>? configure = null
    )
    {
        var containerBuilder = new ContainerBuilder();
        var serviceProviderFactory = new AbpAutofacServiceProviderFactory(containerBuilder);
        builder.Services.AddObjectAccessor(containerBuilder);
        builder.ConfigureContainer(serviceProviderFactory, configure);
        return builder;
    }
}
