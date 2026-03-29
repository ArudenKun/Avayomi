using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avayomi.Navigation.Extensions;

/// <summary>
///     Provides extension methods for safe navigation, similar to Prism's RequestNavigate.
/// </summary>
public static class NavigationHostExtensions
{
    /// <summary>
    ///     Attempts to navigate to the specified content type, with optional retry if host is not ready.
    ///     Similar to Prism's RequestNavigate pattern.
    /// </summary>
    /// <param name="navigationHostManager">The host manager instance.</param>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="contentType">The type of content to navigate to.</param>
    /// <param name="parameter">Optional parameter to pass to the view model.</param>
    /// <param name="onComplete">Optional callback invoked when navigation completes (success or failure).</param>
    /// <param name="retryOnHostNotReady">If true, retries navigation after a short delay when host is not registered.</param>
    public static void RequestNavigate(
        this INavigationHostManager navigationHostManager,
        string hostName,
        Type contentType,
        object? parameter = null,
        Action<NavigationResult>? onComplete = null,
        bool retryOnHostNotReady = true
    )
    {
        if (navigationHostManager == null)
            throw new ArgumentNullException(nameof(navigationHostManager));

        if (string.IsNullOrWhiteSpace(hostName))
            throw new ArgumentException(
                "Host name cannot be null or whitespace.",
                nameof(hostName)
            );

        if (contentType == null)
            throw new ArgumentNullException(nameof(contentType));

        // Check if host exists
        if (!navigationHostManager.HostExists(hostName))
        {
            if (retryOnHostNotReady)
            {
                // Retry after ensuring UI is loaded
                Dispatcher.UIThread.InvokeAsync(
                    () =>
                    {
                        if (navigationHostManager.HostExists(hostName))
                        {
                            PerformNavigation(
                                navigationHostManager,
                                hostName,
                                contentType,
                                parameter,
                                onComplete
                            );
                        }
                        else
                        {
                            // Final attempt after another delay
                            Dispatcher.UIThread.InvokeAsync(
                                () =>
                                    PerformNavigation(
                                        navigationHostManager,
                                        hostName,
                                        contentType,
                                        parameter,
                                        onComplete
                                    ),
                                DispatcherPriority.Loaded
                            );
                        }
                    },
                    DispatcherPriority.Loaded
                );
            }
            else
            {
                onComplete?.Invoke(
                    new NavigationResult
                    {
                        Success = false,
                        Error = new InvalidOperationException(
                            $"No host registered with name '{hostName}'."
                        ),
                    }
                );
            }
            return;
        }

        // Host exists, navigate immediately
        PerformNavigation(navigationHostManager, hostName, contentType, parameter, onComplete);
    }

    /// <summary>
    ///     Attempts to navigate to the specified content type, with optional retry if host is not ready.
    /// </summary>
    /// <typeparam name="T">The type of content to navigate to.</typeparam>
    /// <param name="navigationHostManager">The host manager instance.</param>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="parameter">Optional parameter to pass to the view model.</param>
    /// <param name="onComplete">Optional callback invoked when navigation completes.</param>
    /// <param name="retryOnHostNotReady">If true, retries navigation after a short delay when host is not registered.</param>
    public static void RequestNavigate<T>(
        this INavigationHostManager navigationHostManager,
        string hostName,
        object? parameter = null,
        Action<NavigationResult>? onComplete = null,
        bool retryOnHostNotReady = true
    ) =>
        RequestNavigate(
            navigationHostManager,
            hostName,
            typeof(T),
            parameter,
            onComplete,
            retryOnHostNotReady
        );

    /// <summary>
    ///     Asynchronously attempts to navigate to the specified content type.
    /// </summary>
    /// <param name="navigationHostManager">The host manager instance.</param>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="contentType">The type of content to navigate to.</param>
    /// <param name="parameter">Optional parameter to pass to the view model.</param>
    /// <param name="retryOnHostNotReady">If true, retries navigation after a short delay when host is not registered.</param>
    /// <returns>A task that represents the navigation result.</returns>
    public static async Task<NavigationResult> RequestNavigateAsync(
        this INavigationHostManager navigationHostManager,
        string hostName,
        Type contentType,
        object? parameter = null,
        bool retryOnHostNotReady = true
    )
    {
        if (navigationHostManager == null)
            throw new ArgumentNullException(nameof(navigationHostManager));

        if (string.IsNullOrWhiteSpace(hostName))
            throw new ArgumentException(
                "Host name cannot be null or whitespace.",
                nameof(hostName)
            );

        if (contentType == null)
            throw new ArgumentNullException(nameof(contentType));

        // Check if host exists
        if (!navigationHostManager.HostExists(hostName))
        {
            if (retryOnHostNotReady)
            {
                // Wait for UI thread and retry
                await Dispatcher.UIThread.InvokeAsync(
                    () => Task.CompletedTask,
                    DispatcherPriority.Loaded
                );

                // Check again after waiting
                if (!navigationHostManager.HostExists(hostName))
                {
                    return new NavigationResult
                    {
                        Success = false,
                        Error = new InvalidOperationException(
                            $"No host registered with name '{hostName}' after waiting for host initialization."
                        ),
                    };
                }
            }
            else
            {
                return new NavigationResult
                {
                    Success = false,
                    Error = new InvalidOperationException(
                        $"No host registered with name '{hostName}'."
                    ),
                };
            }
        }

        return await PerformNavigationAsync(
            navigationHostManager,
            hostName,
            contentType,
            parameter
        );
    }

    /// <summary>
    ///     Asynchronously attempts to navigate to the specified content type.
    /// </summary>
    /// <typeparam name="T">The type of content to navigate to.</typeparam>
    /// <param name="navigationHostManager">The host manager instance.</param>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="parameter">Optional parameter to pass to the view model.</param>
    /// <param name="retryOnHostNotReady">If true, retries navigation after a short delay when host is not registered.</param>
    /// <returns>A task that represents the navigation result.</returns>
    public static Task<NavigationResult> RequestNavigateAsync<T>(
        this INavigationHostManager navigationHostManager,
        string hostName,
        object? parameter = null,
        bool retryOnHostNotReady = true
    )
    {
        return RequestNavigateAsync(
            navigationHostManager,
            hostName,
            typeof(T),
            parameter,
            retryOnHostNotReady
        );
    }

    private static void PerformNavigation(
        INavigationHostManager navigationHostManager,
        string hostName,
        Type contentType,
        object? parameter,
        Action<NavigationResult>? onComplete
    )
    {
        try
        {
            navigationHostManager.Navigate(hostName, contentType, parameter);
            onComplete?.Invoke(new NavigationResult { Success = true });
        }
        catch (Exception ex)
        {
            onComplete?.Invoke(new NavigationResult { Success = false, Error = ex });
        }
    }

    private static async Task<NavigationResult> PerformNavigationAsync(
        INavigationHostManager navigationHostManager,
        string hostName,
        Type contentType,
        object? parameter
    )
    {
        try
        {
            await navigationHostManager.NavigateAsync(hostName, contentType, parameter);
            return new NavigationResult { Success = true };
        }
        catch (Exception ex)
        {
            return new NavigationResult { Success = false, Error = ex };
        }
    }
}
