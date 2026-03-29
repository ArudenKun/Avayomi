using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avayomi.Hosting.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Avayomi.Hosting;

public static class HostBuilderExtensions
{
    /// <summary>
    /// Adds Avalonia main window to the host's service collection,
    /// and a <see cref="AppBuilder"/> to create the Avalonia application.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">The application builder, also used by the previewer.</param>
    /// <returns>The updated host application builder.</returns>
    public static IHostBuilder UseAvaloniaHosting<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApplication
    >(this IHostBuilder builder, Action<IServiceProvider, AppBuilder> configure)
        where TApplication : Application =>
        builder.ConfigureServices(services =>
            services
                .AddSingleton<TApplication>()
                .AddSingleton<Application>(sp => sp.GetRequiredService<TApplication>())
                .AddSingleton(sp =>
                {
                    var appBuilder = AppBuilder.Configure(sp.GetRequiredService<TApplication>);
                    configure(sp, appBuilder);
                    return appBuilder;
                })
                .AddSingleton<IClassicDesktopStyleApplicationLifetime>(_ =>
                    (IClassicDesktopStyleApplicationLifetime?)
                        Application.Current?.ApplicationLifetime
                    ?? throw new InvalidOperationException(
                        "Avalonia application lifetime is not set."
                    )
                )
                .AddSingleton<AvaloniaThread>()
                .AddHostedService<AvaloniaHostedService>()
        );

    /// <summary>
    /// Adds Avalonia main window to the host's service collection,
    /// and a <see cref="AppBuilder"/> to create the Avalonia application.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">The application builder, also used by the previewer.</param>
    /// <returns>The updated host application builder.</returns>
    public static IHostBuilder UseAvaloniaHosting<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApp
    >(this IHostBuilder builder, Action<AppBuilder> configure)
        where TApp : Application =>
        builder.UseAvaloniaHosting<TApp>((_, appBuilder) => configure(appBuilder));
}
