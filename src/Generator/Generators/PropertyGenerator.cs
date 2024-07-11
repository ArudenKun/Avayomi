using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Generator.Attributes;
using Generator.Extensions;
using Generator.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.Generators;

[Generator]
internal sealed class PropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
            IsSyntaxTarget,
            GetSyntaxTarget
        );

        var compilationProvider = context.CompilationProvider.Combine(syntaxProvider.Collect());
        context.RegisterImplementationSourceOutput(
            compilationProvider,
            (sourceProductionContext, provider) =>
                OnExecute(sourceProductionContext, provider.Left, provider.Right)
        );
    }

    private static void OnExecute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> nodes
    )
    {
        var targetSymbol = compilation.GetTypeByMetadataName(MetadataNames.ObservableObject);

        var viewModelSymbols = GetAll<INamedTypeSymbol>(nodes, compilation)
            .Where(x => !x.IsAbstract)
            .Where(x => !x.HasAttribute<IgnoreAttribute>())
            .Where(x => x.Name.EndsWith("ViewModel"))
            .Where(x => x.IsOfBaseType(targetSymbol!))
            .OrderBy(x => x.ToDisplayString())
            .ToArray();

        foreach (var viewModelSymbol in viewModelSymbols)
        {
            var viewSymbol = GetView(viewModelSymbol, compilation);
            if (viewSymbol is null)
                continue;

            var source = new SourceStringBuilder(viewSymbol);

            source.PartialTypeBlockBrace(() =>
            {
                source.Line(
                    $"public {viewModelSymbol.ToDisplayString()} ViewModel {{ get; init; }}"
                );
            });

            context.AddSource($"{viewSymbol.ToDisplayString()}.Property.g.cs", source.ToString());
        }
    }

    private static bool IsSyntaxTarget(SyntaxNode node, CancellationToken _)
    {
        return node is ClassDeclarationSyntax;
    }

    private static ClassDeclarationSyntax GetSyntaxTarget(
        GeneratorSyntaxContext context,
        CancellationToken _
    )
    {
        return (ClassDeclarationSyntax)context.Node;
    }

    private static INamedTypeSymbol? GetView(ISymbol symbol, Compilation compilation)
    {
        var viewName = symbol.ToDisplayString().Replace("ViewModel", "View");
        var viewSymbol = compilation.GetTypeByMetadataName(viewName);

        if (viewSymbol is not null)
            return viewSymbol;

        viewName = symbol.ToDisplayString().Replace(".ViewModels.", ".Views.");
        viewName = viewName.Remove(viewName.IndexOf("ViewModel", StringComparison.Ordinal));
        return compilation.GetTypeByMetadataName(viewName);
    }

    private static IEnumerable<TSymbol> GetAll<TSymbol>(
        IEnumerable<SyntaxNode> syntaxNodes,
        Compilation compilation
    )
        where TSymbol : ISymbol
    {
        foreach (var syntaxNode in syntaxNodes)
            if (syntaxNode is FieldDeclarationSyntax fieldDeclaration)
            {
                var semanticModel = compilation.GetSemanticModel(fieldDeclaration.SyntaxTree);

                foreach (var variable in fieldDeclaration.Declaration.Variables)
                {
                    if (semanticModel.GetDeclaredSymbol(variable) is not TSymbol symbol)
                        continue;

                    yield return symbol;
                }
            }
            else
            {
                var semanticModel = compilation.GetSemanticModel(syntaxNode.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(syntaxNode) is not TSymbol symbol)
                    continue;

                yield return symbol;
            }
    }
}
