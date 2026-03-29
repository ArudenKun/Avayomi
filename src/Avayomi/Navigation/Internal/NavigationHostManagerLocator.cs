using System.Collections.Generic;

namespace Avayomi.Navigation.Internal;

/// <summary>
///     Service locator for HostManager. Provides global access to the HostManager instance.
///     This is used internally by NavigationHost to automatically resolve the HostManager.
/// </summary>
internal static class NavigationHostManagerLocator
{
    private static INavigationHostManager? _instance;
    private static readonly List<PendingRegistration> PendingRegistrations = [];

    /// <summary>
    ///     Gets or sets the current HostManager instance.
    /// </summary>
    public static INavigationHostManager? Current
    {
        get => _instance;
        set
        {
            _instance = value;

            // Process any pending registrations when HostManager becomes available
            if (_instance != null)
            {
                ProcessPendingRegistrations(_instance);
            }
        }
    }

    /// <summary>
    ///     Checks if a HostManager instance is available.
    /// </summary>
    public static bool IsInitialized => _instance != null;

    /// <summary>
    ///     Registers a pending host registration that will be processed when HostManager becomes available.
    /// </summary>
    /// <param name="navigationHost">The NavigationHost to register.</param>
    /// <param name="hostName">The name for the host.</param>
    internal static void RegisterPending(NavigationHost navigationHost, string hostName)
    {
        // If HostManager is already available, register immediately
        if (_instance != null)
        {
            _instance.RegisterHost(hostName, navigationHost);
            if (_instance is NavigationHostManager hostManager)
            {
                NavigationHostManager.SetHostManager(navigationHost, hostManager);
            }
            return;
        }

        // Otherwise, add to pending list
        PendingRegistrations.Add(new PendingRegistration(navigationHost, hostName));
    }

    /// <summary>
    ///     Processes all pending host registrations.
    /// </summary>
    /// <param name="navigationHostManager">The HostManager to register hosts with.</param>
    private static void ProcessPendingRegistrations(INavigationHostManager navigationHostManager)
    {
        if (PendingRegistrations.Count == 0)
            return;

        foreach (var pending in PendingRegistrations)
        {
            navigationHostManager.RegisterHost(pending.HostName, pending.NavigationHost);
            if (navigationHostManager is NavigationHostManager concreteManager)
            {
                NavigationHostManager.SetHostManager(pending.NavigationHost, concreteManager);
            }
        }

        PendingRegistrations.Clear();
    }

    /// <summary>
    ///     Resets the current HostManager instance (useful for testing).
    /// </summary>
    internal static void Reset()
    {
        _instance = null;
        PendingRegistrations.Clear();
    }

    /// <summary>
    ///     Represents a pending host registration.
    /// </summary>
    private class PendingRegistration
    {
        public NavigationHost NavigationHost { get; }
        public string HostName { get; }

        public PendingRegistration(NavigationHost navigationHost, string hostName)
        {
            NavigationHost = navigationHost;
            HostName = hostName;
        }
    }
}
