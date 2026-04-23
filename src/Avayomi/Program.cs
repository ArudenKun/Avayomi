using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Declarative;
using Avalonia.Markup.Xaml.Styling;
using Avayomi;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Services.Settings;
using Avayomi.Settings;
using Avayomi.Views;
using Flowery;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Volo.Abp;
using Volo.Abp.IO;
using Volo.Abp.Modularity.PlugIns;

var settingsService = new SettingsService(
    Options.Create(new SettingsServiceOptions()),
    NullLogger<SettingsService>.Instance
);

var loggingLevelSwitch = new LoggingLevelSwitch(
    settingsService.Get<LoggingSettings>().LogEventLevel
);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(loggingLevelSwitch)
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

var abpApplication = await AbpApplicationFactory.CreateAsync<AvayomiModule>(options =>
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

await abpApplication.InitializeAsync();

var lifetime = new ClassicDesktopStyleApplicationLifetime
{
    Args = args,
    ShutdownMode = ShutdownMode.OnMainWindowClose,
};

AppBuilder
    .Configure<Application>()
    .AfterSetup(builder =>
    {
        if (builder.Instance is not { } application)
            return;

        application.Styles.Add(new DaisyUITheme());
        application.Styles.Add(
            new StyleInclude((Uri?)null)
            {
                Source = new Uri("avares://AsyncImageLoader.Avalonia/AdvancedImage.axaml"),
            }
        );
    })
    .UsePlatformDetect()
    .UseR3(ex =>
        abpApplication
            .ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("R3")
            .LogError(ex, "An Unhandled R3 Exception occurred")
    )
    .UseHotReload()
    .UseServiceProvider(abpApplication.ServiceProvider)
    .UseComponentControlFactory(type =>
        (Control)ActivatorUtilities.GetServiceOrCreateInstance(abpApplication.ServiceProvider, type)
    )
    .UseViewInitializationStrategy(ViewInitializationStrategy.Lazy)
    .LogToTrace()
    .LogToDelegate(s =>
        abpApplication
            .ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Avalonia")
            .LogWarning("{Log}", s)
    )
    .With(new SkiaOptions { MaxGpuResourceSizeBytes = (long)512.Megabytes().Bytes })
    .SetupWithLifetime(lifetime);

lifetime.MainWindow = new Window()
    .Title(AvayomiCoreConsts.Name)
    .Width(1280)
    .Height(720)
    .Icon(AvaloniaResources.logo_ico.AsWindowIcon())
    .Content(ViewFactory.Create<ShellView>());

lifetime.Exit += (_, _) =>
{
    abpApplication.Shutdown();
    abpApplication.Dispose();
};

return lifetime.Start(args);
