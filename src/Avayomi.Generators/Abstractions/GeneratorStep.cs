using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Avayomi.Generators.Abstractions;

internal abstract class GeneratorStep
{
    private readonly object _lock = new();

    public GeneratorStepContext Context { get; private set; }

    public void Initialize(GeneratorExecutionContext context, Compilation compilation)
    {
        Context = new GeneratorStepContext(context, compilation);
    }

    public virtual void OnInitialize(Compilation compilation, GeneratorStep[] steps) { }

    public abstract void Execute();

    protected SyntaxTree AddSource(string name, string source)
    {
        var syntaxTree = SyntaxFactory.ParseSyntaxTree(source, Context.Context.ParseOptions);
        Context.Context.AddSource(name, SourceText.From(source, Encoding.UTF8));

        lock (_lock)
        {
            Context = Context with { Compilation = Context.Compilation.AddSyntaxTrees(syntaxTree) };
        }

        return syntaxTree;
    }

    protected void ReportDiagnostic(DiagnosticDescriptor diagnosticDescriptor, Location location) =>
        Context.Context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, location));

    protected SemanticModel GetSemanticModel(SyntaxTree syntaxTree) =>
        Context.Compilation.GetSemanticModel(syntaxTree);

    protected INamedTypeSymbol GetTypeByMetadataName(string fullName) =>
        Context.Compilation.GetTypeByMetadataName(fullName);

    protected IEnumerable<TSymbol> GetAll<TSymbol>(IEnumerable<SyntaxNode> syntaxNodes)
        where TSymbol : ISymbol
    {
        foreach (var syntaxNode in syntaxNodes)
        {
            if (syntaxNode is FieldDeclarationSyntax fieldDeclaration)
            {
                var semanticModel = GetSemanticModel(fieldDeclaration.SyntaxTree);

                foreach (var variable in fieldDeclaration.Declaration.Variables)
                {
                    if (semanticModel.GetDeclaredSymbol(variable) is not TSymbol symbol)
                    {
                        continue;
                    }

                    yield return symbol;
                }
            }
            else
            {
                var semanticModel = GetSemanticModel(syntaxNode.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(syntaxNode) is not TSymbol symbol)
                {
                    continue;
                }

                yield return symbol;
            }
        }
    }
}
