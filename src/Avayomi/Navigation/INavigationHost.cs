namespace Avayomi.Navigation;

/// <summary>
///     Defines the basic navigation service interface.
///     Provides methods for navigating between views.
///     Note: Stack functionality has been removed. CanGoBack always returns false and StackCount always returns 0.
/// </summary>
public interface INavigationHost
{
    /// <summary>
    ///     Gets the current content being displayed.
    /// </summary>
    object? CurrentContent { get; }

    /// <summary>
    ///     Navigates to the specified content.
    /// </summary>
    /// <param name="content">The content to navigate to.</param>
    void SetContent(object content);
}
