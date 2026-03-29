using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avayomi.Hosting.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Avayomi.Hosting;

public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Avalonia main window to the host's service collection,
    /// and a <see cref="AppBuilder"/> to create the Avalonia application.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">The application builder, also used by the previewer.</param>
    /// <returns>The updated host application builder.</returns>
    public static IHostApplicationBuilder AddAvaloniaHosting<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApplication
    >(this HostApplicationBuilder builder, Action<IServiceProvider, AppBuilder> configure)
        where TApplication : Application
    {
        builder
            .Services.AddSingleton<TApplication>()
            .AddSingleton<Application>(sp => sp.GetRequiredService<TApplication>())
            .AddSingleton(sp =>
            {
                var appBuilder = AppBuilder.Configure(sp.GetRequiredService<TApplication>);
                configure(sp, appBuilder);
                return appBuilder;
            })
            .AddSingleton<IClassicDesktopStyleApplicationLifetime>(_ =>
                (IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime
                ?? throw new InvalidOperationException("Avalonia application lifetime is not set.")
            )
            .AddSingleton<AvaloniaThread>()
            .AddHostedService<AvaloniaHostedService>();
        return builder;
    }

    /// <summary>
    /// Adds Avalonia main window to the host's service collection,
    /// and a <see cref="AppBuilder"/> to create the Avalonia application.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">The application builder, also used by the previewer.</param>
    /// <returns>The updated host application builder.</returns>
    public static IHostApplicationBuilder AddAvaloniaHosting<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApp
    >(this HostApplicationBuilder builder, Action<AppBuilder> configure)
        where TApp : Application =>
        builder.AddAvaloniaHosting<TApp>((_, appBuilder) => configure(appBuilder));
}
