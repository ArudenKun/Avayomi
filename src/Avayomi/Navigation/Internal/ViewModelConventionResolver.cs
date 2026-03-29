using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Avayomi.Navigation.Internal;

/// <summary>
///     Internal implementation of convention-based view model resolver.
///     Automatically resolves view model types from view types based on naming conventions.
///     Supports explicit mappings that take precedence over convention-based resolution.
/// </summary>
internal sealed class ViewModelConventionResolver : IViewModelConventionResolver
{
    private const string ViewSuffix = "View";
    private const string ViewModelSuffix = "ViewModel";

    private readonly IViewModelMappingRegistry _mappingRegistry;

    // Thread-safe cache to store resolved ViewModels (and null results).
    private readonly ConcurrentDictionary<Type, CacheEntry> _cache = new();

    /// <summary>
    ///     Initializes a new instance of the ViewModelConventionResolver.
    /// </summary>
    /// <param name="mappingRegistry">The mapping registry for explicit View-ViewModel mappings.</param>
    public ViewModelConventionResolver(IViewModelMappingRegistry mappingRegistry)
    {
        _mappingRegistry =
            mappingRegistry ?? throw new ArgumentNullException(nameof(mappingRegistry));
    }

    /// <summary>
    ///     Attempts to resolve the view model type for a given view type.
    ///     Returns from cache if available; otherwise falls back to explicit mappings and conventions.
    /// </summary>
    /// <param name="viewType">The view type.</param>
    /// <returns>The view model type if resolved; otherwise, null.</returns>
    public Type? ResolveViewModelType(Type viewType)
    {
        if (viewType == null)
            throw new ArgumentNullException(nameof(viewType));

        // Get from cache or execute the resolution logic and store the result
        return _cache
            .GetOrAdd(viewType, type => new CacheEntry(ResolveViewModelTypeInternal(type)))
            .ViewModelType;
    }

    /// <summary>
    ///     Clears the internal view model resolution cache.
    ///     Call this if you dynamically register new explicit mappings at runtime.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    /// <summary>
    ///     The core resolution logic moved from the original ResolveViewModelType method.
    /// </summary>
    private Type? ResolveViewModelTypeInternal(Type viewType)
    {
        // 1. Check explicit mapping first (highest priority)
        var explicitMapping = _mappingRegistry.GetViewModelType(viewType);
        if (explicitMapping != null)
            return explicitMapping;

        // 2. Use default convention: ViewName -> ViewModelName
        var viewName = viewType.Name;
        var viewModelName = GetViewModelNameByConvention(viewName);

        if (string.IsNullOrEmpty(viewModelName))
            return null;

        // Search for the view model type

        // 1. Search in the same namespace and assembly as the view
        var viewModelType = SearchInAssembly(viewType.Assembly, viewType.Namespace, viewModelName);
        if (viewModelType != null)
            return viewModelType;

        // 2. Search in all loaded assemblies (if enabled)
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            viewModelType = SearchInAssembly(assembly, null, viewModelName);
            if (viewModelType != null)
                return viewModelType;
        }

        return null;
    }

    /// <summary>
    ///     Checks if a view model type can be resolved for the given view type.
    /// </summary>
    /// <param name="viewType">The view type.</param>
    /// <returns>True if a view model type can be resolved; otherwise, false.</returns>
    public bool CanResolve(Type viewType) =>
        _cache
            .GetOrAdd(viewType, type => new CacheEntry(ResolveViewModelTypeInternal(type)))
            .ViewModelType
            is not null;

    /// <summary>
    ///     Gets the view model name by convention from the view name.
    /// </summary>
    private string? GetViewModelNameByConvention(string viewName)
    {
        if (string.IsNullOrEmpty(viewName))
            return null;

        if (viewName.EndsWith(ViewSuffix, StringComparison.Ordinal))
        {
            var baseName = viewName.Substring(0, viewName.Length - ViewSuffix.Length);
            return baseName + ViewModelSuffix;
        }

        return viewName + ViewModelSuffix;
    }

    /// <summary>
    ///     Searches for a type in the specified assembly and namespace.
    /// </summary>
    private Type? SearchInAssembly(Assembly assembly, string? viewNamespace, string typeName)
    {
        try
        {
            var potentialNamespaces = GetPotentialNamespaces(viewNamespace);

            foreach (var namespaceName in potentialNamespaces)
            {
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    var fullTypeName = $"{namespaceName}.{typeName}";
                    var type = assembly.GetType(fullTypeName, false);
                    if (type != null)
                        return type;
                }
            }

            var types = assembly.GetTypes();
            return types.FirstOrDefault(t => t.Name == typeName);
        }
        catch (ReflectionTypeLoadException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    ///     Gets potential namespaces for ViewModel based on View namespace conventions.
    /// </summary>
    private List<string> GetPotentialNamespaces(string? viewNamespace)
    {
        var namespaces = new List<string>();

        if (string.IsNullOrEmpty(viewNamespace))
            return namespaces;

        namespaces.Add(viewNamespace);

        if (viewNamespace.Contains(".Views"))
            namespaces.Add(viewNamespace.Replace(".Views", ".ViewModels"));

        if (viewNamespace.EndsWith(".Views", StringComparison.Ordinal))
        {
            var baseNamespace = viewNamespace.Substring(0, viewNamespace.Length - 6);
            namespaces.Add(baseNamespace + ".ViewModels");
        }

        if (
            viewNamespace.EndsWith("Views", StringComparison.Ordinal)
            && !viewNamespace.EndsWith(".Views", StringComparison.Ordinal)
        )
        {
            var baseNamespace = viewNamespace.Substring(0, viewNamespace.Length - 5);
            namespaces.Add(baseNamespace + "ViewModels");
        }

        var lastDotIndex = viewNamespace.LastIndexOf('.');
        if (lastDotIndex > 0)
        {
            var parentNamespace = viewNamespace.Substring(0, lastDotIndex);
            if (parentNamespace.EndsWith(".Views", StringComparison.Ordinal))
            {
                var baseParent = parentNamespace.Substring(0, parentNamespace.Length - 6);
                namespaces.Add(baseParent + ".ViewModels");
            }
            else
            {
                namespaces.Add(parentNamespace + ".ViewModels");
            }
        }

        var parts = viewNamespace.Split('.');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "Views")
            {
                var modifiedParts = parts.ToArray();
                modifiedParts[i] = "ViewModels";
                namespaces.Add(string.Join(".", modifiedParts));
            }
        }

        return namespaces.Distinct().ToList();
    }

    // CACHE ADDITION: A simple struct to allow caching of null values without exceptions or wrappers.
    private readonly struct CacheEntry
    {
        public Type? ViewModelType { get; }

        public CacheEntry(Type? viewModelType) => ViewModelType = viewModelType;
    }
}
