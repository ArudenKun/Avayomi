using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoInterfaceAttributes;
using Desktop.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Desktop.Hosting;

[AutoInterface(Inheritance = [typeof(IDisposable)])]
public sealed class HostedServiceManager : IHostedServiceManager
{
    private readonly ILogger<HostedServiceManager> _logger;
    private volatile bool _disposedValue; // To detect redundant calls

    public HostedServiceManager(
        ILogger<HostedServiceManager> logger,
        IEnumerable<IHostedService> hostedServices
    )
    {
        _logger = logger;
        Services.AddRange(hostedServices.Select(x => new HostedService(x, x.GetType().Name)));
    }

    private List<HostedService> Services { get; } = [];

    private object ServicesLock { get; } = new();
    private bool IsStartAllAsyncStarted { get; set; }

    public void Register<T>(Func<T> serviceFactory, string friendlyName)
        where T : class, IHostedService
    {
        Register<T>(serviceFactory(), friendlyName);
    }

    public async Task StartAllAsync(CancellationToken token = default)
    {
        if (IsStartAllAsyncStarted)
            throw new InvalidOperationException("Operation is already started.");

        IsStartAllAsyncStarted = true;

        var exceptions = new List<Exception>();
        var exceptionsLock = new object();

        var tasks = CloneServices()
            .Select(x =>
                x.Service.StartAsync(token)
                    .ContinueWith(y =>
                    {
                        if (y.Exception is null)
                        {
                            _logger.LogInformation("Started {FriendlyName}.", x.FriendlyName);
                        }
                        else
                        {
                            lock (exceptionsLock)
                            {
                                exceptions.Add(y.Exception);
                            }

                            _logger.LogError("Error starting {FriendlyName}.", x.FriendlyName);
                            _logger.LogException(y.Exception);
                        }
                    })
            );

        await Task.WhenAll(tasks).ConfigureAwait(false);

        if (exceptions.Count != 0)
            throw new AggregateException(exceptions);
    }

    /// <remarks>This method does not throw exceptions.</remarks>
    public async Task StopAllAsync(CancellationToken token = default)
    {
        var tasks = CloneServices()
            .Select(x =>
                x.Service.StopAsync(token)
                    .ContinueWith(y =>
                    {
                        if (y.Exception is null)
                        {
                            _logger.LogInformation("Stopped {FriendlyName}.", x.FriendlyName);
                        }
                        else
                        {
                            _logger.LogError("Error stopping {FriendlyName}.", x.FriendlyName);
                            _logger.LogException(y.Exception);
                        }
                    })
            );

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public T? GetOrDefault<T>()
        where T : class, IHostedService
    {
        lock (ServicesLock)
        {
            return Services.SingleOrDefault(x => x.Service is T)?.Service as T;
        }
    }

    public T Get<T>()
        where T : class, IHostedService
    {
        lock (ServicesLock)
        {
            return (T)Services.Single(x => x.Service is T).Service;
        }
    }

    public bool Any<T>()
        where T : class, IHostedService
    {
        lock (ServicesLock)
        {
            return AnyNoLock<T>();
        }
    }

    private void Register<T>(IHostedService service, string friendlyName)
        where T : class, IHostedService
    {
        if (IsStartAllAsyncStarted)
            throw new InvalidOperationException("Services are already started.");

        lock (ServicesLock)
        {
            if (AnyNoLock<T>())
                throw new InvalidOperationException($"{typeof(T).Name} is already registered.");

            Services.Add(new HostedService(service, friendlyName));
        }
    }

    private HostedService[] CloneServices()
    {
        lock (ServicesLock)
        {
            return Services.ToArray();
        }
    }

    private bool AnyNoLock<T>()
        where T : class, IHostedService
    {
        return Services.Any(x => x.Service is T);
    }

    #region IDisposable Support

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
            return;
        if (disposing)
            foreach (var service in CloneServices())
            {
                if (service.Service is not IDisposable disposable)
                    continue;
                disposable.Dispose();
                _logger.LogInformation("Disposed {FriendlyName}.", service.FriendlyName);
            }

        _disposedValue = true;
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
    }

    #endregion IDisposable Support
}
