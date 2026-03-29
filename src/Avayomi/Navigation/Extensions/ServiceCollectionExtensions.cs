using System;
using Avayomi.Navigation.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Avayomi.Navigation.Extensions;

/// <summary>
///     Extension methods for registering navigation services in dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds NavigationHostManager as a singleton service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNavigationHost(this IServiceCollection services)
    {
        // Register internal services
        services.AddSingleton<INavigationHostRegistry, NavigationHostRegistry>();

        // Register mapping registry with a factory that applies all pending mappings
        services.AddSingleton<IViewModelMappingRegistry>(provider =>
        {
            var registry = new ViewModelMappingRegistry();

            // Apply all registered mappings when the registry is created
            var mappingRegistrations = provider.GetServices<IViewModelMappingRegistration>();
            foreach (var registration in mappingRegistrations)
            {
                registration.Register(registry);
            }

            return registry;
        });

        // Register convention resolver that uses the already-initialized registry
        services.AddSingleton<IViewModelConventionResolver, ViewModelConventionResolver>();
        services.AddSingleton<NavigationInstanceFactory>();
        services.AddSingleton<INavigationInstanceFactory>(sp =>
            sp.GetRequiredService<NavigationInstanceFactory>()
        );
        services.AddSingleton<INavigationHostManager, NavigationHostManager>(provider =>
        {
            var hostRegistry = provider.GetRequiredService<INavigationHostRegistry>();
            var conventionResolver = provider.GetRequiredService<IViewModelConventionResolver>();
            var instanceFactory = provider.GetRequiredService<NavigationInstanceFactory>();

            var hostManager = new NavigationHostManager(
                hostRegistry,
                conventionResolver,
                instanceFactory
            );

            // Configure service provider for instance factory
            // ReSharper disable once ConvertTypeCheckPatternToNullCheck
            instanceFactory.ConfigureServiceProvider(provider);

            NavigationHostManagerLocator.Current = hostManager;

            return hostManager;
        });

        return services;
    }
}

/// <summary>
///     Interface to mark services that need to register mappings.
/// </summary>
internal interface IViewModelMappingRegistration
{
    void Register(IViewModelMappingRegistry registry);
}

/// <summary>
///     Internal class to hold mapping configuration that will be applied when accessed.
/// </summary>
internal class ViewModelMappingRegistration : IViewModelMappingRegistration
{
    private readonly Type _viewType;
    private readonly Type _viewModelType;

    public ViewModelMappingRegistration(Type viewType, Type viewModelType)
    {
        _viewType = viewType;
        _viewModelType = viewModelType;
    }

    public void Register(IViewModelMappingRegistry registry)
    {
        registry.RegisterMapping(_viewType, _viewModelType);
    }
}
