using System;

namespace Avayomi.Navigation;

/// <summary>
///     Defines the convention-based view model resolver interface.
///     Automatically resolves view model types from view types based on naming conventions.
/// </summary>
public interface IViewModelConventionResolver
{
    /// <summary>
    ///     Attempts to resolve the view model type for a given view type based on conventions.
    /// </summary>
    /// <param name="viewType">The view type.</param>
    /// <returns>The view model type if resolved by convention; otherwise, null.</returns>
    Type? ResolveViewModelType(Type viewType);

    /// <summary>
    ///     Checks if a view model type can be resolved for the given view type.
    /// </summary>
    /// <param name="viewType">The view type.</param>
    /// <returns>True if a view model type can be resolved; otherwise, false.</returns>
    bool CanResolve(Type viewType);
}
