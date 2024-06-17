using System;
using System.Collections.Immutable;
using System.Linq;
using Avayomi.Generators.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Avayomi.Generators.Abstractions;

internal abstract class GeneratorStepForDeclaredMethodWithAttribute<TAttribute>
    : GeneratorStepForDeclaredMemberWithAttribute<TAttribute, MethodDeclarationSyntax>
    where TAttribute : Attribute
{
    public override void Execute(MethodDeclarationSyntax[] methodDeclarationSyntaxes)
    {
        var methodSymbols = GetAll<IMethodSymbol>(methodDeclarationSyntaxes).ToArray();

#pragma warning disable RS1024
        var groupedMethods = methodSymbols.GroupBy(x => x.ContainingType);
#pragma warning restore RS1024

        foreach (var groupedMethod in groupedMethods)
        {
            var methodTuples = groupedMethod
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
                GenerateFilename(groupedMethod.Key),
                Execute(groupedMethod.Key, methodTuples, new SourceStringBuilder(groupedMethod.Key))
            );
        }
    }

    protected abstract string Execute(
        INamedTypeSymbol classSymbol,
        ImmutableArray<(IMethodSymbol Method, AttributeData Attribute)> methodTuples,
        SourceStringBuilder source
    );
}
