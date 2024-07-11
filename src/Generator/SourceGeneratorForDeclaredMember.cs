using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Generator.Extensions;
using Generator.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using GeneratorContext = Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext;

namespace Generator;

internal abstract class SourceGeneratorForDeclaredMember<TDeclarationSyntax> : IIncrementalGenerator
    where TDeclarationSyntax : MemberDeclarationSyntax
{
    private const string Ext = ".g.cs";
    private const int MaxFileLength = 255;

    // ReSharper disable once StaticMemberInGenericType
    private static readonly char[] InvalidFileNameChars =
    [
        '\"',
        '<',
        '>',
        '|',
        '\0',
        (char)1,
        (char)2,
        (char)3,
        (char)4,
        (char)5,
        (char)6,
        (char)7,
        (char)8,
        (char)9,
        (char)10,
        (char)11,
        (char)12,
        (char)13,
        (char)14,
        (char)15,
        (char)16,
        (char)17,
        (char)18,
        (char)19,
        (char)20,
        (char)21,
        (char)22,
        (char)23,
        (char)24,
        (char)25,
        (char)26,
        (char)27,
        (char)28,
        (char)29,
        (char)30,
        (char)31,
        ':',
        '*',
        '?',
        '\\',
        '/'
    ];

    private Compilation _compilation = null!;

    protected virtual IEnumerable<(string Name, string Source)> StaticSources => [];

    public void Initialize(GeneratorContext context)
    {
        foreach (var (name, source) in StaticSources)
            context.RegisterPostInitializationOutput(x => x.AddSource($"{name}.g.cs", source));

        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
            IsSyntaxTarget,
            GetSyntaxTarget
        );

        var compilationProvider = context
            .CompilationProvider.Combine(syntaxProvider.Collect())
            .Combine(context.AnalyzerConfigOptionsProvider);
        context.RegisterImplementationSourceOutput(
            compilationProvider,
            (sourceProductionContext, provider) =>
                OnExecute(
                    sourceProductionContext,
                    provider.Left.Left,
                    provider.Left.Right,
                    provider.Right
                )
        );
    }

    protected virtual bool IsSyntaxTarget(SyntaxNode node, CancellationToken _)
    {
        return node is TDeclarationSyntax;
    }

    protected virtual TDeclarationSyntax GetSyntaxTarget(
        GeneratorSyntaxContext context,
        CancellationToken _
    )
    {
        return (TDeclarationSyntax)context.Node;
    }

    private void OnExecute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<TDeclarationSyntax> nodes,
        AnalyzerConfigOptionsProvider options
    )
    {
        _compilation = compilation;
        try
        {
            var (fileName, generatedCode) = _GenerateCode(
                compilation,
                nodes,
                options.GlobalOptions
            );

            context.AddSource(fileName, generatedCode);
        }
        catch (Exception e)
        {
            Log.Error(e);
            throw;
        }
    }

    protected abstract (string FileName, string GeneratedCode) GenerateCode(
        Compilation compilation,
        ImmutableArray<TDeclarationSyntax> nodes,
        AnalyzerConfigOptions options
    );

    private (string FileName, string GeneratedCode) _GenerateCode(
        Compilation compilation,
        ImmutableArray<TDeclarationSyntax> nodes,
        AnalyzerConfigOptions options
    )
    {
        try
        {
            return GenerateCode(compilation, nodes, options);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return (null, null)!;
        }
    }

    protected IEnumerable<TSymbol> GetAll<TSymbol>(IEnumerable<SyntaxNode> syntaxNodes)
        where TSymbol : ISymbol
    {
        foreach (var syntaxNode in syntaxNodes)
            if (syntaxNode is FieldDeclarationSyntax fieldDeclaration)
            {
                var semanticModel = _compilation.GetSemanticModel(fieldDeclaration.SyntaxTree);

                foreach (var variable in fieldDeclaration.Declaration.Variables)
                {
                    if (semanticModel.GetDeclaredSymbol(variable) is not TSymbol symbol)
                        continue;

                    yield return symbol;
                }
            }
            else
            {
                var semanticModel = _compilation.GetSemanticModel(syntaxNode.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(syntaxNode) is not TSymbol symbol)
                    continue;

                yield return symbol;
            }
    }

    protected virtual string GenerateFilename(ISymbol symbol)
    {
        var gn = $"{Format(symbol)}{Ext}";
        Log.Debug($"Generated Filename ({gn.Length}): {gn}\n");
        return gn;

        static string Format(ISymbol symbol)
        {
            return string.Join("_", $"{symbol}".Split(InvalidFileNameChars))
                .Truncate(MaxFileLength - Ext.Length);
        }
    }

    protected virtual SyntaxNode Node(TDeclarationSyntax node)
    {
        return node;
    }
}
