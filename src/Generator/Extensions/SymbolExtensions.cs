using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Generator.Extensions;

internal static class SymbolExtensions
{
    public static string NamespaceOrEmpty(this ISymbol symbol) =>
        symbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : string.Join(".", symbol.ContainingNamespace.ConstituentNamespaces);

    public static bool HasAttribute(this ISymbol symbol, string attributeName) =>
        symbol.GetAttributes().Any(x => x.AttributeClass?.Name == attributeName);

    public static bool HasAttribute<TAttribute>(this ISymbol symbol)
        where TAttribute : Attribute => HasAttribute(symbol, typeof(TAttribute).Name);

    public static string ToFullDisplayString(this ISymbol s) =>
        s.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static string AsCommaSeparated<T>(this IEnumerable<T> items, string? suffixes = null) =>
        string.Join($",{suffixes}", items);

    public static bool IsOfBaseType(this ITypeSymbol symbol, string type)
    {
        var baseType = symbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == type)
                return true;

            baseType = baseType.BaseType;
        }

        return false;
    }

    public static bool IsOfBaseType(this ITypeSymbol? type, ITypeSymbol baseType)
    {
        if (type is ITypeParameterSymbol p)
            return p.ConstraintTypes.Any(ct => ct.IsOfBaseType(baseType));

        while (type != null)
        {
            if (SymbolEqualityComparer.Default.Equals(type, baseType))
                return true;

            type = type.BaseType;
        }

        return false;
    }

    public static IEnumerable<INamedTypeSymbol> CollectTypeSymbols(
        this INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol? targetSymbol
    )
    {
        if (targetSymbol is null)
        {
            foreach (var x1 in Enumerable.Empty<INamedTypeSymbol>())
                yield return x1;
            yield break;
        }

        foreach (
            var namedTypeSymbol in namespaceSymbol
                .GetTypeMembers()
                .Where(x => IsDerivedFrom(x, targetSymbol))
        )
        {
            yield return namedTypeSymbol;
        }

        // Recursively collect types from nested namespaces
        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var nestedTypeSymbol in nestedNamespace.CollectTypeSymbols(targetSymbol))
                yield return nestedTypeSymbol;
        }

        yield break;

        static bool IsDerivedFrom(INamedTypeSymbol? classSymbol, INamedTypeSymbol targetSymbol)
        {
            while (classSymbol != null)
            {
                if (SymbolEqualityComparer.Default.Equals(classSymbol.BaseType, targetSymbol))
                    return true;
                classSymbol = classSymbol.BaseType;
            }

            return false;
        }
    }
}
