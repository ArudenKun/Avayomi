using System;
using System.Collections.Immutable;
using System.Linq;
using Avayomi.Generators.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Avayomi.Generators.Abstractions;

internal abstract class GeneratorStepForDeclaredFieldWithAttribute<TAttribute>
    : GeneratorStepForDeclaredMemberWithAttribute<TAttribute, FieldDeclarationSyntax>
    where TAttribute : Attribute
{
    public override void Execute(FieldDeclarationSyntax[] fieldDeclarationSyntaxes)
    {
        var fieldSymbols = GetAll<IFieldSymbol>(fieldDeclarationSyntaxes).ToArray();

#pragma warning disable RS1024
        var groupedFields = fieldSymbols.GroupBy(x => x.ContainingType);
#pragma warning restore RS1024

        foreach (var groupedField in groupedFields)
        {
            var fieldTuples = groupedField
                .Select(
                    x =>
                        (
                            Field: x,
                            Attribute: x.GetAttributes()
                                .Single(
                                    attributeData =>
                                        attributeData.AttributeClass?.Name == AttributeType
                                )
                        )
                )
                .ToImmutableArray();

            AddSource(
                GenerateFilename(groupedField.Key),
                Execute(groupedField.Key, fieldTuples, new SourceStringBuilder(groupedField.Key))
            );
        }
    }

    protected abstract string Execute(
        INamedTypeSymbol classSymbol,
        ImmutableArray<(IFieldSymbol Field, AttributeData Attribute)> fieldTuples,
        SourceStringBuilder source
    );
}
