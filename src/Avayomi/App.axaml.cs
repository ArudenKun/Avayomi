using System;
using AsyncImageLoader;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Services;
using Avayomi.Utilities;
using Avayomi.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace Avayomi;

public sealed class App : Application, ISingletonDependency
{
    public required IServiceProvider ServiceProvider { private get; init; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var replicantImageLoader = new ReplicantImageLoader(
            AvayomiCoreConsts.Paths.CacheDir.Combine("Images")
        );
        ImageLoader.AsyncImageLoader = replicantImageLoader;
        ImageBrushLoader.AsyncImageLoader = replicantImageLoader;

        ServiceProvider.GetRequiredService<ILoggingService>().Initialize();

        DataTemplates.Add(ServiceProvider.GetRequiredService<ViewLocator>());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var mainWindow =
            (Window?)
                DataTemplates[0].Build(ServiceProvider.GetRequiredService<MainWindowViewModel>())
            ?? throw new InvalidOperationException("Could not find Main Window");
        TopLevel topLevel = desktop.MainWindow = mainWindow;
        ServiceProvider.GetRequiredService<ObjectAccessor<TopLevel>>().Value = topLevel;

        base.OnFrameworkInitializationCompleted();
    }
}
