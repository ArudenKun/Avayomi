using System;
using System.Linq;
using Avayomi.Generators.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Avayomi.Generators.Abstractions;

internal abstract class GeneratorStepForDeclaredTypeWithAttribute<TAttribute>
    : GeneratorStepForDeclaredMemberWithAttribute<TAttribute, TypeDeclarationSyntax>
    where TAttribute : Attribute
{
    public override void Execute(TypeDeclarationSyntax[] typeDeclarationSyntaxes)
    {
        var typeSymbols = GetAll<INamedTypeSymbol>(typeDeclarationSyntaxes).ToArray();

        foreach (var typeSymbol in typeSymbols)
        {
            var attribute = typeSymbol
                .GetAttributes()
                .Single(attributeData => attributeData.AttributeClass?.Name == AttributeType);

            AddSource(
                GenerateFilename(typeSymbol),
                Execute(typeSymbol, attribute, new SourceStringBuilder(typeSymbol))
            );
        }
    }

    protected abstract string Execute(
        INamedTypeSymbol classSymbol,
        AttributeData attributeData,
        SourceStringBuilder source
    );
}
