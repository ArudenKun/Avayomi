using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AsyncNavigation.Abstractions;
using Autofac;
using Autofac.Core.Resolving.Pipeline;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Core.GraphQL;
using Avayomi.Hosting;
using Avayomi.Messaging;
using Avayomi.Providers;
using Avayomi.Services.Settings;
using Avayomi.ViewModels;
using Avayomi.Views;
using CommunityToolkit.Mvvm.Messaging;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using NeoSmart.Caching.Sqlite;
using R3;
using R3.ObservableEvents;
using ServiceScan.SourceGenerator;
using SQLitePCL;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using ZiggyCreatures.Caching.Fusion;

namespace Avayomi;

[DependsOn(typeof(AvayomiCoreModule), typeof(AvayomiProvidersModule), typeof(AvayomiHostingModule))]
public sealed partial class AvayomiModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureMessengerHandlers(context.Services);
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<SettingsServiceOptions>(options =>
            options.FilePath = AvayomiCoreConsts.Paths.SettingsPath
        );

        context.Services.AddNavigationSupport();
        RegisterViewAndViewModels(context.Services);
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
                        new JsonStringEnumConverter(),
                        new JsonStringEnumMemberConverter(),
                    },
                }
            )
            .TryWithAutoSetup();
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        //context.ServiceProvider.GetRequiredService<ISettingsService>().Save();
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

    [ScanForTypes(
        AssignableTo = typeof(IView<>),
        Handler = nameof(RegisterViewAndViewModelHandler)
    )]
    private static partial void RegisterViewAndViewModels(IServiceCollection services);

    private static void RegisterViewAndViewModelHandler<TView, TViewModel>(
        IServiceCollection services
    )
        where TView : Control, IView
        where TViewModel : ViewModel
    {
        var viewType = typeof(TView);
        var viewName = viewType.Name;
        var viewAttribute = viewType.GetSingleAttributeOrNull<ViewAttribute>(false);
        if (viewAttribute is not null)
        {
            viewName = viewAttribute.Name;
        }

        services.RegisterView<TView, TViewModel>(viewName);
    }
}
