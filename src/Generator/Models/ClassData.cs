using H.Generators.Extensions;
using Microsoft.CodeAnalysis;

namespace Generator.Models;

internal readonly record struct ClassData(
    string Namespace,
    string Name,
    string FullName,
    string Type,
    INamedTypeSymbol TypeSymbol,
    string Modifiers,
    bool IsStatic,
    EquatableArray<string> Methods
);
