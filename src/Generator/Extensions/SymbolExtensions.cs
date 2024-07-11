using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.Extensions;

internal static class SymbolExtensions
{
    public static string NamespaceOrEmpty(this ISymbol symbol) =>
        symbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : string.Join(".", symbol.ContainingNamespace.ConstituentNamespaces);

    public static string? NamespaceOrNull(this ISymbol symbol) =>
        symbol.ContainingNamespace.IsGlobalNamespace
            ? null
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
    }

    public static bool IsDerivedFrom(
        this INamedTypeSymbol? classSymbol,
        INamedTypeSymbol targetSymbol
    )
    {
        while (classSymbol != null)
        {
            if (SymbolEqualityComparer.Default.Equals(classSymbol.BaseType, targetSymbol))
                return true;
            classSymbol = classSymbol.BaseType;
        }

        return false;
    }

    public static bool IsSpecialType(this ITypeSymbol symbol) =>
        symbol is IArrayTypeSymbol
        || symbol.SpecialType != SpecialType.None
        || (
            symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && symbol.BaseType != null
            && symbol.BaseType.SpecialType != SpecialType.None
        );

    public static bool IsSpecialType(this ITypeSymbol symbol, SpecialType specialType) =>
        symbol.SpecialType == specialType;

    public static IEnumerable<IMethodSymbol> GetImplementedPartialMethods(
        this ITypeSymbol typeSymbol,
        Func<IMethodSymbol, bool>? predicate = null
    )
    {
        var partialMethods = typeSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(x => x.IsPartialMethodImplemented());

        return predicate is not null ? partialMethods.Where(predicate) : partialMethods;
    }

    public static IMethodSymbol? GetImplementedPartialMethod(
        this ITypeSymbol typeSymbol,
        string methodSignature
    ) =>
        typeSymbol
            .GetImplementedPartialMethods()
            .FirstOrDefault(x => x.ToDisplayString().Contains(methodSignature));

    public static bool IsPartialMethodImplemented(
        this ITypeSymbol typeSymbol,
        string methodSignature
    ) => typeSymbol.GetImplementedPartialMethod(methodSignature) is not null;

    public static bool IsPartialMethodImplemented(this IMethodSymbol method)
    {
        if (method.IsDefinedInMetadata())
            return true;

        foreach (var r in method.DeclaringSyntaxReferences)
        {
            if (r.GetSyntax() is not MethodDeclarationSyntax node)
                continue;
            if (node.Body != null || !node.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                return true;
        }

        return false;
    }

    /// <returns>False if it's not defined in source</returns>
    public static bool IsDefinedInMetadata(this ISymbol symbol) =>
        symbol.Locations.Any(loc => loc.IsInMetadata);
}
