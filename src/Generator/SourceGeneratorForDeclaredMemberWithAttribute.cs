using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Generator.Extensions;
using Generator.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Generator;

internal abstract class SourceGeneratorForDeclaredMemberWithAttribute<
    TAttribute,
    TDeclarationSyntax
> : IIncrementalGenerator
    where TAttribute : Attribute
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

    private static string AttributeType { get; } = typeof(TAttribute).Name;

    // ReSharper disable once StaticMemberInGenericType
    private static string AttributeName { get; } =
        Regex.Replace(AttributeType, "Attribute$", "", RegexOptions.Compiled);

    protected virtual IEnumerable<(string Name, string Source)> StaticSources => [];

    public void Initialize(IncrementalGeneratorInitializationContext context)
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
        return;

        static bool IsSyntaxTarget(SyntaxNode node, CancellationToken _)
        {
            return node is TDeclarationSyntax type && HasAttributeType();

            bool HasAttributeType()
            {
                if (type.AttributeLists.Count is 0)
                    return false;

                return type
                    .AttributeLists.SelectMany(attributeList => attributeList.Attributes)
                    .Any(attribute => attribute.Name.ToString() == AttributeName);
            }
        }

        static TDeclarationSyntax GetSyntaxTarget(
            GeneratorSyntaxContext context,
            CancellationToken _
        )
        {
            return (TDeclarationSyntax)context.Node;
        }

        void OnExecute(
            SourceProductionContext sourceProductionContext,
            Compilation compilation,
            ImmutableArray<TDeclarationSyntax> nodes,
            AnalyzerConfigOptionsProvider options
        )
        {
            try
            {
                foreach (var node in nodes.Distinct())
                {
                    if (sourceProductionContext.CancellationToken.IsCancellationRequested)
                        return;

                    var model = compilation.GetSemanticModel(node.SyntaxTree);
                    var symbol = model.GetDeclaredSymbol(Node(node));

                    var attribute = symbol
                        ?.GetAttributes()
                        .SingleOrDefault(x => x.AttributeClass?.Name == AttributeType);

                    if (attribute is null)
                        continue;

                    if (symbol is null)
                        continue;

                    var (generatedCode, error) = _GenerateCode(
                        compilation,
                        node,
                        symbol,
                        attribute,
                        options.GlobalOptions
                    );

                    if (generatedCode is null)
                    {
                        var descriptor = new DiagnosticDescriptor(
                            error.Id,
                            error.Title,
                            error.Message,
                            error.Category,
                            DiagnosticSeverity.Error,
                            true
                        );
                        var diagnostic = Diagnostic.Create(
                            descriptor,
                            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                        );
                        sourceProductionContext.ReportDiagnostic(diagnostic);
                        continue;
                    }

                    sourceProductionContext.AddSource(GenerateFilename(symbol), generatedCode);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }
    }

    protected abstract (string? GeneratedCode, DiagnosticDetail Error) GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        ISymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    );

    private (string? GeneratedCode, DiagnosticDetail Error) _GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        ISymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    )
    {
        try
        {
            return GenerateCode(compilation, node, symbol, attribute, options);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return (null, InternalError(e));
        }

        static DiagnosticDetail InternalError(Exception e)
        {
            return new DiagnosticDetail { Title = "Internal Error", Message = e.Message };
        }
    }

    protected virtual string GenerateFilename(ISymbol symbol)
    {
        var gn = $"{Format()}{Ext}";
        Log.Debug($"Generated Filename ({gn.Length}): {gn}\n");
        return gn;

        string Format() =>
            string.Join(
                    "_",
                    $"{symbol}.{GetType().Name.Replace(nameof(Generator), "")}".Split(
                        InvalidFileNameChars
                    )
                )
                .Truncate(MaxFileLength - Ext.Length);
    }

    protected virtual SyntaxNode Node(TDeclarationSyntax node)
    {
        return node;
    }
}
