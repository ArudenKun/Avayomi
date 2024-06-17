using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Avayomi.Generators.Utilities;

internal static class SymbolExtensions
{
    public static string NamespaceOrNull(this ISymbol symbol) =>
        symbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : string.Join(".", symbol.ContainingNamespace.ConstituentNamespaces);

    public static bool HasAttribute(this ISymbol symbol, string attributeName) =>
        symbol.GetAttributes().Any(x => x.AttributeClass?.Name == attributeName);

    public static string ToFullDisplayString(this ISymbol s) =>
        s.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static string AsCommaSeparated<T>(this IEnumerable<T> items, string suffixes = null) =>
        string.Join($",{suffixes}", items);

    public static bool InheritsFrom(this ITypeSymbol symbol, string type)
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

    public static bool IsOfBaseType(this ITypeSymbol type, ITypeSymbol baseType)
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

    public static string GetExplicitDefaultValueString(this IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
        {
            return null;
        }

        return parameter.ExplicitDefaultValue switch
        {
            string s => $"\"{s}\"",
            null => "null",
            _ => parameter.ExplicitDefaultValue.ToString()
        };
    }

    public static IEnumerable<INamedTypeSymbol> CollectTypeSymbols(
        this INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol targetSymbol
    )
    {
        foreach (
            var namedTypeSymbol in namespaceSymbol
                .GetTypeMembers()
                .Where(x => !x.IsAbstract)
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

        static bool IsDerivedFrom(INamedTypeSymbol classSymbol, INamedTypeSymbol targetSymbol)
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
