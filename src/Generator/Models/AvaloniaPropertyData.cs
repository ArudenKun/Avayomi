using System;
using System.Linq;
using Avalonia.Data;
using Generator.Attributes;
using Generator.Extensions;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;

namespace Generator.Models;

internal readonly record struct AvaloniaPropertyData(
    string Name,
    string Type,
    INamedTypeSymbol TypeSymbol,
    string ShortType,
    bool IsValueType,
    bool IsSpecialType,
    string? DefaultValue,
    bool IsReadOnly,
    bool IsDirect,
    bool IsAttached,
    bool IsAddOwner,
    string[] PropertyAttributes,
    INamedTypeSymbol[] PropertyAttributeSymbols,
    string? DefaultBindingMode
);

internal static class Extensions
{
    public static AvaloniaPropertyData GetAvaloniaPropertyData(
        this AttributeData attribute,
        bool isAddOwner = false,
        bool isAttached = false
    )
    {
        attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));

        var name =
            attribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString() ?? string.Empty;
        var typeSymbol =
            attribute.GetGenericTypeArgument(0) as INamedTypeSymbol
            ?? attribute.ConstructorArguments.ElementAtOrDefault(1).Value as INamedTypeSymbol;
        var type =
            typeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty;
        var shortType =
            typeSymbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            ?? string.Empty;
        var isValueType = typeSymbol?.IsValueType ?? true;
        var isSpecialType = typeSymbol.IsSpecialType() ?? false;
        var defaultValue =
            attribute
                .GetNamedArgument(nameof(AvaloniaPropertyAttribute.DefaultValueExpression))
                .Value?.ToString()
            ?? attribute
                .GetNamedArgument(nameof(AvaloniaPropertyAttribute.DefaultValue))
                .Value?.ToString();

        var isReadOnly = attribute
            .GetNamedArgument(nameof(AvaloniaPropertyAttribute.IsReadOnly))
            .ToBoolean();
        var isDirect = attribute
            .GetNamedArgument(nameof(AvaloniaPropertyAttribute.IsDirect))
            .ToBoolean();

        var propertyAttributeSymbols = attribute.GetArgumentArray<INamedTypeSymbol>(
            nameof(AvaloniaPropertyAttribute.PropertyAttributes)
        );

        var propertyAttributes = propertyAttributeSymbols
            .Select(x => x.ToFullDisplayString())
            .ToArray();

        var defaultBindingMode = attribute
            .GetNamedArgument(nameof(AvaloniaPropertyAttribute.DefaultBindingMode))
            .ToEnum<BindingMode>()
            ?.ToString("G");

        return new AvaloniaPropertyData(
            Name: name,
            Type: type,
            TypeSymbol: typeSymbol
                ?? throw new ArgumentNullException(nameof(typeSymbol), "typeSymbol is null"),
            ShortType: shortType,
            IsValueType: isValueType,
            IsSpecialType: isSpecialType,
            DefaultValue: defaultValue,
            IsReadOnly: isReadOnly,
            IsDirect: isDirect,
            IsAttached: isAttached,
            IsAddOwner: isAddOwner,
            PropertyAttributes: propertyAttributes,
            PropertyAttributeSymbols: propertyAttributeSymbols,
            DefaultBindingMode: defaultBindingMode
        );
    }
}
