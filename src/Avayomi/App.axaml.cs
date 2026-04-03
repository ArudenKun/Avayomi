using System;
using AsyncImageLoader;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Utilities;
using ZLinq;

namespace Avayomi;

public sealed class App : Application
{
    public static new App Current =>
        (App?)Application.Current
        ?? throw new InvalidOperationException("Applications is not yet initialized.");

    public static TopLevel TopLevel { get; internal set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var replicantImageLoader = new ReplicantImageLoader(
            AvayomiCoreConsts.Paths.CacheDir.Combine("Images")
        );
        ImageLoader.AsyncImageLoader = replicantImageLoader;
        ImageBrushLoader.AsyncImageLoader = replicantImageLoader;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime)
            return;

        // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
        // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
        DisableAvaloniaDataAnnotationValidation();
        base.OnFrameworkInitializationCompleted();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove = BindingPlugins
            .DataValidators.AsValueEnumerable()
            .OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
