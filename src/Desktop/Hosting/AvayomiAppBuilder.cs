using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using CommunityToolkit.Mvvm.DependencyInjection;
using Core.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Desktop.Hosting;

public class AvayomiAppBuilder
{
    private readonly ServiceCollection _services = [];
    private AvayomiAppOptions _avayomiAppOptions;

    public AvayomiAppBuilder(string[] args)
    {
        _avayomiAppOptions = new AvayomiAppOptions(args);
        Services.AddSingleton<IHostedServiceManager, HostedServiceManager>();
        Logging = InitializeLogging();
    }

    public IServiceCollection Services => _services;

    public ILoggingBuilder Logging { get; }

    public AvayomiAppBuilder ConfigureAvayomiApp<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApplication
    >(Action<AppBuilder>? configureAppBuilder = null)
        where TApplication : Application
    {
        _avayomiAppOptions = _avayomiAppOptions with
        {
            ConfigureAppBuilderDelegate = configureAppBuilder
        };
        Services.TryAddSingleton<TApplication>();
        Services.TryAddSingleton<Application>(sp => sp.GetRequiredService<TApplication>());

        return this;
    }

    public AvayomiAppBuilder ConfigureSingleInstance(string id, string? name = null)
    {
        _avayomiAppOptions = _avayomiAppOptions with
        {
            MutexId = id,
            MutexName = name ?? EnvironmentHelper.AppFriendlyName
        };
        return this;
    }

    public AvayomiApp Build()
    {
        Services.TryAddSingleton(_avayomiAppOptions);

        var sp = _services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(sp);
        return new AvayomiApp(sp);
    }

    private LoggingBuilder InitializeLogging()
    {
        Services.AddLogging();
        return new LoggingBuilder(Services);
    }

    private sealed class LoggingBuilder(IServiceCollection services) : ILoggingBuilder
    {
        public IServiceCollection Services { get; } = services;
    }
}
