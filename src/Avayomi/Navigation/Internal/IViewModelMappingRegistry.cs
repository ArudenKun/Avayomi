using System;
using System.Collections.Generic;

namespace Avayomi.Navigation.Internal;

/// <summary>
///     Defines the interface for managing explicit View-ViewModel mappings.
///     Explicit mappings take precedence over convention-based resolution.
/// </summary>
internal interface IViewModelMappingRegistry
{
    /// <summary>
    ///     Registers an explicit mapping between a View type and ViewModel type.
    /// </summary>
    /// <typeparam name="TView">The View type.</typeparam>
    /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
    void RegisterMapping<TView, TViewModel>();

    /// <summary>
    ///     Registers an explicit mapping between a View type and ViewModel type.
    /// </summary>
    /// <param name="viewType">The View type.</param>
    /// <param name="viewModelType">The ViewModel type.</param>
    /// <exception cref="ArgumentNullException">Thrown when viewType or viewModelType is null.</exception>
    void RegisterMapping(Type viewType, Type viewModelType);

    /// <summary>
    ///     Gets the explicitly mapped ViewModel type for the specified View type.
    /// </summary>
    /// <param name="viewType">The View type.</param>
    /// <returns>The mapped ViewModel type if exists; otherwise, null.</returns>
    Type? GetViewModelType(Type viewType);

    /// <summary>
    ///     Checks if an explicit mapping exists for the specified View type.
    /// </summary>
    /// <param name="viewType">The View type.</param>
    /// <returns>True if a mapping exists; otherwise, false.</returns>
    bool HasMapping(Type viewType);

    /// <summary>
    ///     Removes the explicit mapping for the specified View type.
    /// </summary>
    /// <param name="viewType">The View type.</param>
    /// <returns>True if the mapping was removed; false if no mapping existed.</returns>
    bool RemoveMapping(Type viewType);

    /// <summary>
    ///     Clears all explicit mappings.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Gets all registered View-ViewModel mappings.
    /// </summary>
    /// <returns>A read-only dictionary of View types to ViewModel types.</returns>
    IReadOnlyDictionary<Type, Type> GetAllMappings();
}
