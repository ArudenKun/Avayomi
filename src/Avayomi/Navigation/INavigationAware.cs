namespace Avayomi.Navigation;

/// <summary>
///     Interface for ViewModels that need to receive navigation parameters and participate in navigation lifecycle.
///     Implement this interface to handle navigation events and control navigation flow.
/// </summary>
public interface INavigationAware
{
    /// <summary>
    ///     Called before navigation occurs to confirm if navigation should proceed.
    ///     Return true to allow navigation, false to cancel.
    /// </summary>
    /// <param name="parameter">The navigation parameter that will be passed if navigation proceeds.</param>
    /// <returns>True to allow navigation, false to cancel navigation.</returns>
    bool CanNavigateTo(object? parameter);

    /// <summary>
    ///     Called when the view is navigated to, with an optional parameter.
    /// </summary>
    /// <param name="parameter">The navigation parameter, or null if none was provided.</param>
    void OnNavigatedTo(object? parameter);

    /// <summary>
    ///     Called before navigating away from the current view to confirm if navigation away should proceed.
    ///     Return true to allow navigation away, false to cancel and stay on current view.
    ///     This is useful for confirming unsaved changes before leaving.
    /// </summary>
    /// <returns>True to allow navigation away, false to cancel navigation and stay on current view.</returns>
    bool CanNavigateFrom();

    /// <summary>
    ///     Called when navigating away from the current view.
    ///     This allows the ViewModel to perform cleanup or save state.
    /// </summary>
    void OnNavigatedFrom();
}
