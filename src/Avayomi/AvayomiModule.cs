using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autofac;
using Autofac.Core.Resolving.Pipeline;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avayomi.Core;
using Avayomi.Core.Dependency;
using Avayomi.Core.Extensions;
using Avayomi.Core.GraphQL;
using Avayomi.Messaging;
using Avayomi.Navigation.Extensions;
using Avayomi.Providers;
using Avayomi.Services.Settings;
using Avayomi.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using NeoSmart.Caching.Sqlite;
using R3;
using R3.ObservableEvents;
using SQLitePCL;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using ZiggyCreatures.Caching.Fusion;

namespace Avayomi;

[DependsOn(typeof(AvayomiCoreModule), typeof(AvayomiProvidersModule))]
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
        context.Services.AddHttpClient();

        context.Services.AddSqliteCache(
            AvayomiCoreConsts.Paths.CacheDir.Combine("cache.db"),
            new SQLite3Provider_e_sqlite3()
        );
        context
            .Services.AddFusionCache()
            .WithDefaultEntryOptions(options => options.SetFailSafe(true, 2.Hours(), 2.Minutes()))
            .WithSystemTextJsonSerializer(
                new JsonSerializerOptions
                {
                    TypeInfoResolver = new GqlObjectTypeInfoResolver(),
                    Converters =
                    {
                        new JsonStringEnumMemberConverter(),
                        new JsonStringEnumConverter(),
                    },
                }
            )
            .TryWithAutoSetup();
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

                            if (
                                context.NewInstanceActivated && instance is IInitializer initializer
                            )
                                initializer.Initialize();

                            var messenger = WeakReferenceMessenger.Default;
                            messenger.RegisterAll(instance);
                            messenger.RegisterAllReceivers(instance);
                        }
                    )
            );
}
