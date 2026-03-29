using System;

namespace Avayomi.Navigation;

/// <summary>
///     Represents the result of a navigation operation.
/// </summary>
public class NavigationResult
{
    /// <summary>
    ///     Gets or sets whether the navigation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Gets or sets the error that occurred during navigation, if any.
    /// </summary>
    public Exception? Error { get; set; }
}
