using System.Collections.Generic;

namespace Avayomi.Navigation;

/// <summary>
///     Platform-agnostic interface for host registry service.
///     Manages registration and retrieval of navigation hosts.
/// </summary>
public interface INavigationHostRegistry
{
    /// <summary>
    ///     Registers a navigation host with the specified host name.
    /// </summary>
    /// <param name="hostName">The unique name for the host.</param>
    /// <param name="host">The navigation host to register.</param>
    void RegisterHost(string hostName, INavigationHost host);

    /// <summary>
    ///     Unregisters a host with the specified name.
    /// </summary>
    /// <param name="hostName">The name of the host to unregister.</param>
    /// <returns>True if the host was successfully unregistered; otherwise, false.</returns>
    bool UnregisterHost(string hostName);

    /// <summary>
    ///     Gets a navigation host by host name.
    /// </summary>
    /// <param name="hostName">The name of the host.</param>
    /// <returns>The navigation host, or null if not found.</returns>
    INavigationHost? GetHost(string hostName);

    /// <summary>
    ///     Gets all registered host names.
    /// </summary>
    /// <returns>A collection of registered host names.</returns>
    IEnumerable<string> GetHostNames();
}
