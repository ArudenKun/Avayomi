using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avayomi.Navigation.Internal;

namespace Avayomi.Navigation;

/// <summary>
///     Manages navigation hosts in the application, similar to Prism's RegionManager.
///     Provides attached properties to register NavigationHost controls with names in XAML.
///     Supports view-viewmodel registration with DI container integration.
///     Uses convention-based resolution to automatically map views to viewmodels.
///     Must be configured with a DI container via dependency injection.
///     Acts as a facade coordinating specialized internal services.
/// </summary>
public class NavigationHostManager : INavigationHostManager
{
    /// <summary>
    ///     Defines the HostName attached property.
    /// </summary>
    public static readonly AttachedProperty<string?> HostNameProperty =
        AvaloniaProperty.RegisterAttached<NavigationHostManager, Control, string?>("HostName");

    /// <summary>
    ///     Defines the HostManager attached property.
    /// </summary>
    public static readonly AttachedProperty<NavigationHostManager?> HostManagerProperty =
        AvaloniaProperty.RegisterAttached<NavigationHostManager, Control, NavigationHostManager?>(
            "HostManager"
        );

    private readonly INavigationHostRegistry _navigationHostRegistry;
    private readonly IViewModelConventionResolver _conventionResolver;
    private readonly NavigationInstanceFactory _navigationInstanceFactory;

    static NavigationHostManager()
    {
        HostNameProperty.Changed.AddClassHandler<Control>(OnHostNameChanged);
        HostManagerProperty.Changed.AddClassHandler<Control>(OnHostManagerChanged);
    }

    /// <summary>
    ///     Initializes a new instance of the HostManager with custom service implementations.
    ///     Used for dependency injection scenarios.
    /// </summary>
    internal NavigationHostManager(
        INavigationHostRegistry navigationHostRegistry,
        IViewModelConventionResolver conventionResolver,
        NavigationInstanceFactory navigationInstanceFactory
    )
    {
        _navigationHostRegistry =
            navigationHostRegistry
            ?? throw new ArgumentNullException(nameof(navigationHostRegistry));
        _conventionResolver =
            conventionResolver ?? throw new ArgumentNullException(nameof(conventionResolver));
        _navigationInstanceFactory =
            navigationInstanceFactory
            ?? throw new ArgumentNullException(nameof(navigationInstanceFactory));
    }

    /// <summary>
    ///     Registers a navigation host with the specified host name.
    /// </summary>
    /// <param name="hostName">The unique name for the host.</param>
    /// <param name="host">The navigation host to register.</param>
    public void RegisterHost(string hostName, INavigationHost host)
    {
        _navigationHostRegistry.RegisterHost(hostName, host);
    }

    /// <summary>
    ///     Unregisters a host with the specified name.
    /// </summary>
    /// <param name="hostName">The name of the host to unregister.</param>
    /// <returns>True if the host was successfully unregistered; otherwise, false.</returns>
    public bool UnregisterHost(string hostName)
    {
        return _navigationHostRegistry.UnregisterHost(hostName);
    }

    /// <summary>
    ///     Gets a navigation host by host name.
    /// </summary>
    /// <param name="hostName">The name of the host.</param>
    /// <returns>The navigation host, or null if not found.</returns>
    public INavigationHost? GetHost(string hostName)
    {
        return _navigationHostRegistry.GetHost(hostName);
    }

    /// <summary>
    ///     Gets a platform-specific navigation host by host name (internal helper).
    /// </summary>
    /// <param name="hostName">The name of the host.</param>
    /// <returns>The navigation host, or null if not found.</returns>
    private NavigationHost? GetPlatformHost(string hostName)
    {
        var host = _navigationHostRegistry.GetHost(hostName);
        return host as NavigationHost;
    }

    /// <summary>
    ///     Gets a platform-specific navigation host by host name and throws if not found.
    /// </summary>
    /// <param name="hostName">The name of the host.</param>
    /// <returns>The navigation host.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no host is registered with the specified name.</exception>
    private NavigationHost GetHostOrThrow(string hostName)
    {
        var host = GetPlatformHost(hostName);
        if (host == null)
            throw new InvalidOperationException($"No host registered with name '{hostName}'.");
        return host;
    }

    /// <summary>
    ///     Gets all registered host names.
    /// </summary>
    /// <returns>A collection of registered host names.</returns>
    public IEnumerable<string> GetHostNames()
    {
        return _navigationHostRegistry.GetHostNames();
    }

    /// <summary>
    ///     Checks if a host with the specified name exists.
    /// </summary>
    /// <param name="hostName">The name of the host to check.</param>
    /// <returns>True if the host exists; otherwise, false.</returns>
    public bool HostExists(string hostName)
    {
        return GetHost(hostName) != null;
    }

    /// <summary>
    ///     Navigates to the specified content in a host.
    /// </summary>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="content">The content to navigate to.</param>
    public void Navigate(string hostName, object content)
    {
        var host = GetPlatformHost(hostName);
        if (host == null)
            throw new InvalidOperationException($"No host registered with name '{hostName}'.");

        if (!(content is Control controlContent))
            throw new ArgumentException(
                "Content must be a Control for Avalonia platform.",
                nameof(content)
            );

        var currentContent = host.CurrentContent;

        // Navigation lifecycle: Check if current ViewModel allows navigation away
        // Only check if DataContext is not inherited from parent (NavigationHost)
        if (
            currentContent != null
            && currentContent.DataContext != null
            && currentContent.DataContext != host.DataContext
            && currentContent.DataContext is INavigationAware currentNavigationAware
        )
        {
            if (!currentNavigationAware.CanNavigateFrom())
                // Navigation cancelled by current ViewModel
                return;

            currentNavigationAware.OnNavigatedFrom();
        }

        host.Navigate(controlContent);
    }

    /// <summary>
    ///     Navigates to the specified content type with optional parameters in a host.
    ///     Automatically resolves and sets up view model using convention-based resolution.
    /// </summary>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="contentType">The type of content to navigate to.</param>
    /// <param name="parameter">Optional parameter to pass to the view model or content.</param>
    public void Navigate(string hostName, Type contentType, object? parameter = null)
    {
        if (contentType == null)
            throw new ArgumentNullException(nameof(contentType));

        var host = GetHostOrThrow(hostName);

        // Use convention-based resolution to find ViewModel
        var viewModelType = _conventionResolver.ResolveViewModelType(contentType);

        if (viewModelType != null)
        {
            // Use view-viewmodel navigation
            NavigateWithViewModelInternal(host, contentType, viewModelType, parameter);
        }
        else
        {
            // Create control instance directly (no view model)
            if (!typeof(Control).IsAssignableFrom(contentType))
                throw new ArgumentException(
                    "Content type must derive from Control",
                    nameof(contentType)
                );

            var content = _navigationInstanceFactory.CreateView(contentType);

            Navigate(hostName, content);
        }
    }

    /// <summary>
    ///     Navigates to the specified content type in a host.
    ///     Automatically resolves and sets up view model if a mapping is registered.
    /// </summary>
    /// <typeparam name="T">The type of content to navigate to.</typeparam>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="parameter">Optional parameter to pass to the view model or content.</param>
    public void Navigate<T>(string hostName, object? parameter = null)
    {
        Navigate(hostName, typeof(T), parameter);
    }

    /// <summary>
    ///     Asynchronously navigates to the specified content in a host.
    /// </summary>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="content">The content to navigate to.</param>
    /// <returns>A task representing the asynchronous navigation operation.</returns>
    public async System.Threading.Tasks.Task NavigateAsync(string hostName, object content)
    {
        var host = GetPlatformHost(hostName);
        if (host == null)
            throw new InvalidOperationException($"No host registered with name '{hostName}'.");

        if (!(content is Control controlContent))
            throw new ArgumentException(
                "Content must be a Control for Avalonia platform.",
                nameof(content)
            );

        var currentContent = host.CurrentContent;

        // Navigation lifecycle: Check if current ViewModel allows navigation away
        // Only check if DataContext is not inherited from parent (NavigationHost)
        if (
            currentContent != null
            && currentContent.DataContext != null
            && currentContent.DataContext != host.DataContext
        )
        {
            // Check async interface first
            if (currentContent.DataContext is IAsyncNavigationAware currentAsyncNavigationAware)
            {
                if (!await currentAsyncNavigationAware.CanNavigateFromAsync())
                    return; // Navigation cancelled by current ViewModel

                await currentAsyncNavigationAware.OnNavigatedFromAsync();
            }
            else if (currentContent.DataContext is INavigationAware currentNavigationAware)
            {
                if (!currentNavigationAware.CanNavigateFrom())
                    return; // Navigation cancelled by current ViewModel

                currentNavigationAware.OnNavigatedFrom();
            }
        }

        host.Navigate(controlContent);
    }

    /// <summary>
    ///     Asynchronously navigates to the specified content type with optional parameters in a host.
    /// </summary>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="contentType">The type of content to navigate to.</param>
    /// <param name="parameter">Optional parameter to pass to the view model or content.</param>
    /// <returns>A task representing the asynchronous navigation operation.</returns>
    public async System.Threading.Tasks.Task NavigateAsync(
        string hostName,
        Type contentType,
        object? parameter = null
    )
    {
        if (contentType == null)
            throw new ArgumentNullException(nameof(contentType));

        var host = GetHostOrThrow(hostName);

        // Use convention-based resolution to find ViewModel
        var viewModelType = _conventionResolver.ResolveViewModelType(contentType);

        if (viewModelType != null)
        {
            // Use view-viewmodel navigation
            await NavigateWithViewModelInternalAsync(host, contentType, viewModelType, parameter);
        }
        else
        {
            // Create control instance directly (no view model)
            if (!typeof(Control).IsAssignableFrom(contentType))
                throw new ArgumentException(
                    "Content type must derive from Control",
                    nameof(contentType)
                );

            var content = _navigationInstanceFactory.CreateView(contentType);

            await NavigateAsync(hostName, content);
        }
    }

    /// <summary>
    ///     Asynchronously navigates to the specified content type in a host.
    /// </summary>
    /// <typeparam name="T">The type of content to navigate to.</typeparam>
    /// <param name="hostName">The name of the host to navigate in.</param>
    /// <param name="parameter">Optional parameter to pass to the view model or content.</param>
    /// <returns>A task representing the asynchronous navigation operation.</returns>
    public System.Threading.Tasks.Task NavigateAsync<T>(string hostName, object? parameter = null)
    {
        return NavigateAsync(hostName, typeof(T), parameter);
    }

    /// <summary>
    ///     Configures the service provider for dependency injection.
    ///     This must be called before any navigation operations.
    /// </summary>
    /// <param name="serviceProvider">The service provider to use for resolving dependencies.</param>
    public void ConfigureServices(IServiceProvider serviceProvider)
    {
        _navigationInstanceFactory.ConfigureServiceProvider(
            serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider))
        );
    }

    /// <summary>
    ///     Gets the HostName attached property value.
    /// </summary>
    /// <param name="control">The control to get the property from.</param>
    /// <returns>The host name.</returns>
    public static string? GetHostName(Control control)
    {
        return control.GetValue(HostNameProperty);
    }

    /// <summary>
    ///     Sets the HostName attached property value.
    /// </summary>
    /// <param name="control">The control to set the property on.</param>
    /// <param name="value">The host name.</param>
    public static void SetHostName(Control control, string? value)
    {
        control.SetValue(HostNameProperty, value);
    }

    /// <summary>
    ///     Gets the HostManager attached property value.
    /// </summary>
    /// <param name="control">The control to get the property from.</param>
    /// <returns>The host manager instance.</returns>
    public static NavigationHostManager? GetHostManager(Control control)
    {
        return control.GetValue(HostManagerProperty);
    }

    /// <summary>
    ///     Sets the HostManager attached property value.
    /// </summary>
    /// <param name="control">The control to set the property on.</param>
    /// <param name="value">The host manager instance.</param>
    public static void SetHostManager(Control control, NavigationHostManager? value)
    {
        control.SetValue(HostManagerProperty, value);
    }

    private static void OnHostNameChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (!(control is NavigationHost host))
            throw new InvalidOperationException(
                $"HostName can only be set on NavigationHost controls. Attempted to set on {control.GetType().Name}"
            );

        var oldName = e.OldValue as string;
        var newName = e.NewValue as string;

        // Get the host manager - try attached property first, then fall back to locator
        var hostManager = GetHostManager(control);

        if (hostManager == null)
        {
            // Try to get from locator
            if (
                NavigationHostManagerLocator.IsInitialized
                && NavigationHostManagerLocator.Current != null
            )
            {
                var current = NavigationHostManagerLocator.Current;
                if (current is NavigationHostManager locatorManager)
                {
                    hostManager = locatorManager;
                    SetHostManager(control, locatorManager);
                }
            }

            if (hostManager == null)
            {
                // HostManager not yet available, register as pending
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    NavigationHostManagerLocator.RegisterPending(host, newName!);
                }
                return;
            }
        }

        // Unregister old name if it exists
        if (!string.IsNullOrWhiteSpace(oldName))
            hostManager.UnregisterHost(oldName!);

        // Register new name if it exists
        if (!string.IsNullOrWhiteSpace(newName))
            hostManager.RegisterHost(newName!, host);
    }

    private static void OnHostManagerChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (!(control is NavigationHost host))
            return;

        var oldManager = e.OldValue as NavigationHostManager;
        var newManager = e.NewValue as NavigationHostManager;
        var hostName = GetHostName(control);

        // Unregister from old manager if exists
        if (oldManager != null && !string.IsNullOrWhiteSpace(hostName))
        {
            oldManager.UnregisterHost(hostName!);
        }

        // Register with new manager if exists and HostName is set
        if (newManager != null && !string.IsNullOrWhiteSpace(hostName))
        {
            newManager.RegisterHost(hostName!, host);
        }
    }

    /// <summary>
    ///     Internal method to handle navigation with view-viewmodel resolution.
    /// </summary>
    /// <param name="navigationHost">The navigation host.</param>
    /// <param name="viewType">The type of view to navigate to.</param>
    /// <param name="viewModelType">The type of view model to create.</param>
    /// <param name="parameter">Optional parameter to pass to the view model.</param>
    private void NavigateWithViewModelInternal(
        NavigationHost navigationHost,
        Type viewType,
        Type viewModelType,
        object? parameter
    )
    {
        var currentContent = navigationHost.CurrentContent;

        // Navigation lifecycle: Check if current ViewModel allows navigation away
        // Only check if DataContext is not inherited from parent (NavigationHost)
        if (
            currentContent is { DataContext: { } }
            && currentContent.DataContext != navigationHost.DataContext
            && currentContent.DataContext is INavigationAware currentNavigationAware
        )
            if (!currentNavigationAware.CanNavigateFrom())
                // Navigation cancelled by current ViewModel
                return;

        // Create view and viewmodel instances from DI container
        var viewObj = _navigationInstanceFactory.CreateView(viewType);
        var viewModel = _navigationInstanceFactory.CreateViewModel(viewModelType);

        if (!(viewObj is Control view))
            throw new InvalidOperationException("Created view must be a Control");

        // Navigation lifecycle: Request confirmation from target ViewModel
        if (viewModel is INavigationAware targetNavigationAware)
            // Ask the target ViewModel if navigation can proceed
            if (!targetNavigationAware.CanNavigateTo(parameter))
                // Navigation cancelled by target ViewModel
                return;

        // Navigation lifecycle: Notify current ViewModel about navigation away (FIXED)
        // Only notify if DataContext is not inherited from parent
        if (
            currentContent is { DataContext: { } }
            && currentContent.DataContext != navigationHost.DataContext
            && currentContent.DataContext is INavigationAware currentAware
        )
            currentAware.OnNavigatedFrom(); // Now correctly called before navigation

        // Set parameter using INavigationAware interface
        if (viewModel is INavigationAware navigationAware)
            navigationAware.OnNavigatedTo(parameter);

        view.DataContext = viewModel;
        navigationHost.Navigate(view);
    }

    /// <summary>
    ///     Internal async method to handle navigation with view-viewmodel resolution.
    ///     Supports IAsyncNavigationAware for async lifecycle events.
    /// </summary>
    /// <param name="navigationHost">The navigation host.</param>
    /// <param name="viewType">The type of view to navigate to.</param>
    /// <param name="viewModelType">The type of view model to create.</param>
    /// <param name="parameter">Optional parameter to pass to the view model.</param>
    private async System.Threading.Tasks.Task NavigateWithViewModelInternalAsync(
        NavigationHost navigationHost,
        Type viewType,
        Type viewModelType,
        object? parameter
    )
    {
        var currentContent = navigationHost.CurrentContent;

        // Navigation lifecycle: Check if current ViewModel allows navigation away
        // Only check if DataContext is not inherited from parent (NavigationHost)
        if (
            currentContent is { DataContext: { } }
            && currentContent.DataContext != navigationHost.DataContext
        )
        {
            // Check async interface first
            if (currentContent.DataContext is IAsyncNavigationAware currentAsyncNavigationAware)
            {
                if (!await currentAsyncNavigationAware.CanNavigateFromAsync())
                    return; // Navigation cancelled by current ViewModel
            }
            else if (currentContent.DataContext is INavigationAware currentNavigationAware)
            {
                if (!currentNavigationAware.CanNavigateFrom())
                    return; // Navigation cancelled by current ViewModel
            }
        }

        // Create view and viewmodel instances from DI container
        var viewObj = _navigationInstanceFactory.CreateView(viewType);
        var viewModel = _navigationInstanceFactory.CreateViewModel(viewModelType);

        if (!(viewObj is Control view))
            throw new InvalidOperationException("Created view must be a Control");

        // Navigation lifecycle: Request confirmation from target ViewModel
        // Check async interface first
        if (viewModel is IAsyncNavigationAware targetAsyncNavigationAware)
        {
            if (!await targetAsyncNavigationAware.CanNavigateToAsync(parameter))
                return; // Navigation cancelled by target ViewModel
        }
        else if (viewModel is INavigationAware targetNavigationAware)
        {
            if (!targetNavigationAware.CanNavigateTo(parameter))
                return; // Navigation cancelled by target ViewModel
        }

        // Navigation lifecycle: Notify current ViewModel about navigation away
        // Only notify if DataContext is not inherited from parent
        if (
            currentContent is { DataContext: { } }
            && currentContent.DataContext != navigationHost.DataContext
        )
        {
            if (currentContent.DataContext is IAsyncNavigationAware currentAsyncAware)
            {
                await currentAsyncAware.OnNavigatedFromAsync();
            }
            else if (currentContent.DataContext is INavigationAware currentAware)
            {
                currentAware.OnNavigatedFrom();
            }
        }

        // Set parameter using IAsyncNavigationAware or INavigationAware interface
        if (viewModel is IAsyncNavigationAware asyncNavigationAware)
        {
            await asyncNavigationAware.OnNavigatedToAsync(parameter);
        }
        else if (viewModel is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedTo(parameter);
        }

        view.DataContext = viewModel;
        navigationHost.Navigate(view);
    }
}
