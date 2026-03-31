using System;
using Autofac;
using Autofac.Core.Resolving.Pipeline;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avayomi.AniList;
using Avayomi.Core;
using Avayomi.Messaging;
using Avayomi.Navigation.Extensions;
using Avayomi.Services.Settings;
using Avayomi.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using R3;
using R3.ObservableEvents;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Avayomi;

[DependsOn(typeof(AvayomiCoreModule), typeof(AvayomiAniListModule), typeof(AbpCachingModule))]
public sealed class AvayomiModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddConventionalRegistrar(new ViewModelConventionalRegistrar());
        ConfigureMessengerHandlers(context.Services);
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<SettingsServiceOptions>(options =>
            options.FilePath = AvayomiCoreConsts.Paths.SettingsPath
        );

        context.Services.AddNavigationHost();
        context.Services.AddObjectAccessor<TopLevel>();
        context.Services.AddSingleton<TopLevel>(sp =>
            sp.GetRequiredService<IObjectAccessor<TopLevel>>().Value
            ?? throw new InvalidOperationException("Toplevel is not yet initialized")
        );
        context.Services.AddSingleton<IStorageProvider>(sp =>
            sp.GetRequiredService<TopLevel>().StorageProvider
        );
        context.Services.AddSingleton<IClipboard>(sp =>
            sp.GetRequiredService<TopLevel>().Clipboard!
        );
        context.Services.AddSingleton<ILauncher>(sp => sp.GetRequiredService<TopLevel>().Launcher);
        context.Services.AddSingleton<IFocusManager>(sp =>
            sp.GetRequiredService<TopLevel>().FocusManager!
        );
        context.Services.AddSingleton<ISukiDialogManager, SukiDialogManager>();
        context.Services.AddSingleton<ISukiToastManager, SukiToastManager>();

        context.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        context.ServiceProvider.GetRequiredService<ISettingsService>().Save();
    }

    private static void ConfigureMessengerHandlers(IServiceCollection services) =>
        services
            .GetContainerBuilder()
            .ComponentRegistryBuilder.Events()
            .Registered.Subscribe(args =>
                args.ComponentRegistration.PipelineBuilding += (_, builder) =>
                    builder.Use(
                        PipelinePhase.Activation,
                        (context, next) =>
                        {
                            next(context);

                            var instance = context.Instance;
                            if (instance is null)
                                return;

                            var messenger = WeakReferenceMessenger.Default;
                            messenger.RegisterAll(instance);
                            messenger.RegisterAllReceivers(instance);
                        }
                    )
            );
}
