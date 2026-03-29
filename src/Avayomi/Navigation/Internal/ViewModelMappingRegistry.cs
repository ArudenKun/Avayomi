using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Avayomi.Navigation.Internal;

/// <summary>
///     Thread-safe implementation of View-ViewModel mapping registry.
///     Manages explicit mappings between View types and ViewModel types.
/// </summary>
internal sealed class ViewModelMappingRegistry : IViewModelMappingRegistry
{
    private readonly ConcurrentDictionary<Type, Type> _mappings = new();

    /// <summary>
    ///     Registers an explicit mapping between a View type and ViewModel type.
    /// </summary>
    /// <typeparam name="TView">The View type.</typeparam>
    /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
    public void RegisterMapping<TView, TViewModel>()
    {
        RegisterMapping(typeof(TView), typeof(TViewModel));
    }

    /// <summary>
    ///     Registers an explicit mapping between a View type and ViewModel type.
    /// </summary>
    /// <param name="viewType">The View type.</param>
    /// <param name="viewModelType">The ViewModel type.</param>
    /// <exception cref="ArgumentNullException">Thrown when viewType or viewModelType is null.</exception>
    public void RegisterMapping(Type viewType, Type viewModelType)
    {
        if (viewType == null)
            throw new ArgumentNullException(nameof(viewType));

        _mappings[viewType] =
            viewModelType ?? throw new ArgumentNullException(nameof(viewModelType));
    }

    /// <summary>
    ///     Gets the explicitly mapped ViewModel type for the specified View type.
    /// </summary>
    /// <param name="viewType">The View type.</param>
    /// <returns>The mapped ViewModel type if exists; otherwise, null.</returns>
    public Type? GetViewModelType(Type? viewType)
    {
        if (viewType == null)
            return null;

        return _mappings.GetValueOrDefault(viewType);
    }

    /// <summary>
    ///     Checks if an explicit mapping exists for the specified View type.
    /// </summary>
    /// <param name="viewType">The View type.</param>
    /// <returns>True if a mapping exists; otherwise, false.</returns>
    public bool HasMapping(Type? viewType)
    {
        if (viewType == null)
            return false;

        return _mappings.ContainsKey(viewType);
    }

    /// <summary>
    ///     Removes the explicit mapping for the specified View type.
    /// </summary>
    /// <param name="viewType">The View type.</param>
    /// <returns>True if the mapping was removed; false if no mapping existed.</returns>
    public bool RemoveMapping(Type? viewType)
    {
        if (viewType == null)
            return false;

        return _mappings.TryRemove(viewType, out _);
    }

    /// <summary>
    ///     Clears all explicit mappings.
    /// </summary>
    public void Clear()
    {
        _mappings.Clear();
    }

    /// <summary>
    ///     Gets all registered View-ViewModel mappings.
    /// </summary>
    /// <returns>A read-only dictionary of View types to ViewModel types.</returns>
    public IReadOnlyDictionary<Type, Type> GetAllMappings()
    {
        return new Dictionary<Type, Type>(_mappings);
    }
}
