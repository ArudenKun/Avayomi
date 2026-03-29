using System.Threading.Tasks;

namespace Avayomi.Navigation;

/// <summary>
///     Async version of INavigationAware interface for ViewModels that need to perform asynchronous operations
///     during navigation lifecycle events. Implement this interface to handle navigation events asynchronously
///     and control navigation flow with async operations.
/// </summary>
public interface IAsyncNavigationAware
{
    /// <summary>
    ///     Called asynchronously before navigation occurs to confirm if navigation should proceed.
    ///     Return true to allow navigation, false to cancel.
    /// </summary>
    /// <param name="parameter">The navigation parameter that will be passed if navigation proceeds.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true to allow navigation, false to cancel navigation.</returns>
    Task<bool> CanNavigateToAsync(object? parameter);

    /// <summary>
    ///     Called asynchronously when the view is navigated to, with an optional parameter.
    ///     Use this for async initialization like loading data from a database or API.
    /// </summary>
    /// <param name="parameter">The navigation parameter, or null if none was provided.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task OnNavigatedToAsync(object? parameter);

    /// <summary>
    ///     Called asynchronously before navigating away from the current view to confirm if navigation away should proceed.
    ///     Return true to allow navigation away, false to cancel and stay on current view.
    ///     This is useful for confirming unsaved changes before leaving or performing cleanup operations.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains true to allow navigation away, false to cancel navigation and stay on current view.</returns>
    Task<bool> CanNavigateFromAsync();

    /// <summary>
    ///     Called asynchronously when navigating away from the current view.
    ///     This allows the ViewModel to perform async cleanup or save state operations.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task OnNavigatedFromAsync();
}
