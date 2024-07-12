using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.Metadata.Extensions;

internal static class SyntaxValueProviderExtensions
{
    /// <summary>
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
            fullyQualifiedMetadataName,
            static (node, _) =>
                node
                    is ClassDeclarationSyntax { AttributeLists.Count: > 0 }
                        or RecordDeclarationSyntax { AttributeLists.Count: > 0 },
            static (context, _) => context
        );
    }
}
