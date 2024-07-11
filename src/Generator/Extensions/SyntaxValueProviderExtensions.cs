﻿using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Generator.Extensions;

internal static class SyntaxValueProviderExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="source"></param>
    /// <param name="fullyQualifiedMetadataName"></param>
    /// <returns></returns>
    public static IncrementalValuesProvider<GeneratorAttributeSyntaxContext> ForAttributeWithMetadataNameOfClassesAndRecords(
        this SyntaxValueProvider source,
        string fullyQualifiedMetadataName
    )
    {
        return source.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: fullyQualifiedMetadataName,
            predicate: static (node, _) =>
                node
                    is ClassDeclarationSyntax { AttributeLists.Count: > 0, }
                        or RecordDeclarationSyntax { AttributeLists.Count: > 0, },
            transform: static (context, _) => context
        );
    }
}
