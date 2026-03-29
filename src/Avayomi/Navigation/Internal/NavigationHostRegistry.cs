using System;
using System.Collections.Generic;

namespace Avayomi.Navigation.Internal;

/// <summary>
/// Internal implementation of host registry service.
/// Adapts platform-specific NavigationHost to platform-agnostic interface.
/// </summary>
internal sealed class NavigationHostRegistry : INavigationHostRegistry
{
    private readonly Dictionary<string, NavigationHost> _hosts =
        new Dictionary<string, NavigationHost>();

    public void RegisterHost(string hostName, INavigationHost host)
    {
        if (string.IsNullOrWhiteSpace(hostName))
            throw new ArgumentException(
                "Host name cannot be null or whitespace.",
                nameof(hostName)
            );

        if (host == null)
            throw new ArgumentNullException(nameof(host));

        if (!(host is NavigationHost navigationHost))
            throw new ArgumentException(
                "Host must be an Avalonia NavigationHost instance.",
                nameof(host)
            );

        if (_hosts.ContainsKey(hostName))
            UnregisterHost(hostName);

        _hosts[hostName] = navigationHost;
    }

    public bool UnregisterHost(string hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName))
            return false;

        return _hosts.Remove(hostName);
    }

    public INavigationHost? GetHost(string hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName))
            return null;

        return _hosts.TryGetValue(hostName, out var host) ? host : null;
    }

    /// <summary>
    /// Gets the platform-specific NavigationHost by host name.
    /// </summary>
    internal NavigationHost? GetPlatformHost(string hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName))
            return null;

        return _hosts.TryGetValue(hostName, out var host) ? host : null;
    }

    public IEnumerable<string> GetHostNames()
    {
        return _hosts.Keys;
    }
}
