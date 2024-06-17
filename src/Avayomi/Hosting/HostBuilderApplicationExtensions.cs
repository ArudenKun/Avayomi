using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avayomi.Hosting.Ui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Avayomi.Hosting;

public static class HostBuilderApplicationExtensions
{
    /// <summary>
    /// The key used to access the <see cref="UiContext" /> instance in
    /// <see cref="IHostApplicationBuilder.Properties" />.
    /// </summary>
    public const string HostingContextKey = "UserInterfaceHostingContext";

    /// <summary>
    /// Configures the host builder for a Windows UI (WinUI) application.
    /// </summary>
    /// <typeparam name="TApplication">
    /// The concrete type for the <see cref="Application" /> class.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// This method configures the host builder to support a Windows UI (WinUI)
    /// application. It sets up the necessary services, including the hosting
    /// context, user interface thread, and the hosted service for the user
    /// interface.
    /// </para>
    /// <para>
    /// It attempts to find a <see cref="UiContext" /> instance from the
    /// host builder properties and if not available creates one and adds it as
    /// a singleton service and as an <see cref="BaseUiContext" /> service
    /// for use by the <see cref="UiThreadHostedService" />.
    /// </para>
    /// <para>
    /// Upon successful completion, the dependency injector will be able to
    /// provide the single instance of the application as a <typeparamref name="TApplication" />
    /// and as an <see cref="Application" /> if it is not the same type.
    /// </para>
    /// </remarks>
    /// <param name="hostBuilder">
    /// The host builder to which the WinUI service needs to be added.
    /// </param>
    /// <param name="appBuilderDelegate">Configuration for Avalonia AppBuilder</param>
    /// <param name="hostContextKey">The key to use for the host context</param>
    /// <returns>The host builder for chaining calls.</returns>
    /// <exception cref="ArgumentException">
    /// When the application's type does not extend <see cref="Application" />.
    /// </exception>
    public static HostApplicationBuilder ConfigureAvalonia<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TApplication
    >(
        this HostApplicationBuilder hostBuilder,
        Action<AppBuilder>? appBuilderDelegate = null,
        string? hostContextKey = null
    )
        where TApplication : Application
    {
        var context = new UiContext { IsLifetimeLinked = true };
        ((IHostApplicationBuilder)hostBuilder).Properties[hostContextKey ?? HostingContextKey] =
            context;

        _ = hostBuilder.Services.AddSingleton(context);

        _ = hostBuilder
            .Services.AddSingleton<IUiThread, UiThread>()
            .AddHostedService<UiThreadHostedService>();

        hostBuilder.Services.TryAddSingleton<TApplication>();
        _ = hostBuilder.Services.AddSingleton<AppBuilder>(sp =>
        {
            var appBuilder = AppBuilder.Configure(sp.GetRequiredService<TApplication>);
            appBuilderDelegate?.Invoke(appBuilder);
            return appBuilder;
        });

        _ = hostBuilder.Services.AddSingleton<Application>(services =>
            services.GetRequiredService<TApplication>()
        );

        return hostBuilder;
    }
}