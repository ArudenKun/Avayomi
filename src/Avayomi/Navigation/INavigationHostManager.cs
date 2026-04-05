using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avayomi.Navigation;

/// <summary>
///     Defines the host manager service interface.
///     Manages multiple navigation hosts and provides centralized navigation control.
///     Note: Stack functionality has been removed.
/// </summary>
public interface INavigationHostManager
{
    event EventHandler<NavigationHostChangedEventArgs>? HostChanged;

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

    /// <summary>
    ///     Checks if a host with the specified name exists.
    /// </summary>
    /// <param name="hostName">The name of the host to check.</param>
    /// <returns>True if the host exists; otherwise, false.</returns>
    bool HostExists(string hostName);

    /// <summary>
    ///     Navigates to the specified content in a host.
    /// </summary>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="content">The content to navigate to.</param>
    void Navigate(string hostName, object content);

    /// <summary>
    ///     Navigates to the specified content type with optional parameters in a host.
    /// </summary>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="contentType">The type of content to navigate to.</param>
    /// <param name="parameter">Optional parameter to pass to the view model or content.</param>
    void Navigate(string hostName, Type contentType, object? parameter = null);

    /// <summary>
    ///     Navigates to the specified content type in a host.
    /// </summary>
    /// <typeparam name="T">The type of content to navigate to.</typeparam>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="parameter">Optional parameter to pass to the view model or content.</param>
    void Navigate<T>(string hostName, object? parameter = null);

    /// <summary>
    ///     Asynchronously navigates to the specified content in a host.
    /// </summary>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="content">The content to navigate to.</param>
    /// <returns>A task representing the asynchronous navigation operation.</returns>
    Task NavigateAsync(string hostName, object content);

    /// <summary>
    ///     Asynchronously navigates to the specified content type with optional parameters in a host.
    /// </summary>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="contentType">The type of content to navigate to.</param>
    /// <param name="parameter">Optional parameter to pass to the view model or content.</param>
    /// <returns>A task representing the asynchronous navigation operation.</returns>
    Task NavigateAsync(string hostName, Type contentType, object? parameter = null);

    /// <summary>
    ///     Asynchronously navigates to the specified content type in a host.
    /// </summary>
    /// <typeparam name="T">The type of content to navigate to.</typeparam>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="parameter">Optional parameter to pass to the view model or content.</param>
    /// <returns>A task representing the asynchronous navigation operation.</returns>
    Task NavigateAsync<T>(string hostName, object? parameter = null);
}
