using System;

namespace Avayomi.Navigation;

/// <summary>
///     Platform-agnostic interface for creating view and view model instances.
///     Abstracts dependency injection and object instantiation.
/// </summary>
public interface INavigationInstanceFactory
{
    /// <summary>
    ///     Creates a view instance of the specified type.
    /// </summary>
    /// <param name="viewType">The type of view to create.</param>
    /// <returns>The created view instance.</returns>
    object CreateView(Type viewType);

    /// <summary>
    ///     Creates a view model instance of the specified type.
    /// </summary>
    /// <param name="viewModelType">The type of view model to create.</param>
    /// <returns>The created view model instance.</returns>
    object CreateViewModel(Type viewModelType);

    /// <summary>
    ///     Creates an instance of the specified type.
    /// </summary>
    /// <param name="type">The type to create.</param>
    /// <returns>The created instance.</returns>
    object CreateInstance(Type type);
}
