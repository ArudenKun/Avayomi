using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.Extensions;

internal static class SyntaxExtensions
{
    public static string GetLeadingComments(this SyntaxNode node)
    {
        var comments = node.GetLeadingTrivia()
            .Where(x =>
                x.IsKind(SyntaxKind.MultiLineCommentTrivia)
                || x.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || x.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
                || x.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
            );
#pragma warning disable RS1035
        return string.Join(Environment.NewLine, comments);
#pragma warning restore RS1035
    }

    public static IEnumerable<(string Property, string Comment)> GetPropertyComments(
        this SyntaxNode node,
        string strip = "// "
    )
    {
        return node.DescendantNodes()
            .Where(x => x.IsKind(SyntaxKind.PropertyDeclaration))
            .Cast<PropertyDeclarationSyntax>()
            .Select(x => (x.Identifier.ToString(), x.GetLeadingComments().Replace(strip, "")))
            .Where(x => !string.IsNullOrWhiteSpace(x.Item2));
    }

    public static AttributeSyntax? TryFindAttributeSyntax(
        this ClassDeclarationSyntax classSyntax,
        AttributeData attribute
    )
    {
        var name = attribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString();

        return classSyntax
            .AttributeLists.SelectMany(static x => x.Attributes)
            .FirstOrDefault(x =>
                x.ArgumentList?.Arguments.FirstOrDefault()?.ToString().Trim('"').RemoveNameof()
                == name
            );
    }
}
