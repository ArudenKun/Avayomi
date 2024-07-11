﻿using System;
using System.Collections.Generic;
using System.Linq;
using Generator.Attributes;
using Generator.Extensions;
using Generator.Interfaces;
using Generator.Metadata.CopyCode;
using Generator.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Generator.Generators;

[Generator]
internal sealed class StaticViewLocatorGenerator
    : SourceGeneratorForDeclaredTypeWithAttribute<StaticViewLocatorAttribute>
{
    protected override IEnumerable<(string Name, string Source)> StaticSources =>
        [
            (
                $"{MetadataNames.Attributes}.{nameof(StaticViewLocatorAttribute)}.g.cs",
                Copy.GeneratorAttributesStaticViewLocatorAttribute
            ),
            (
                $"{MetadataNames.Attributes}.{nameof(SingletonAttribute)}.g.cs",
                Copy.GeneratorAttributesSingletonAttribute
            ),
            (
                $"{MetadataNames.Attributes}.{nameof(IgnoreAttribute)}.g.cs",
                Copy.GeneratorAttributesIgnoreAttribute
            ),
            (
                $"{MetadataNames.Attributes}.{nameof(IActivatable)}.g.cs",
                Copy.GeneratorInterfacesIActivatable
            )
        ];

    protected override (string GeneratedCode, DiagnosticDetail Error) GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        INamedTypeSymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    )
    {
        var targetSymbol = compilation.GetTypeByMetadataName(MetadataNames.ObservableObject);

        var viewModelSymbols = compilation
            .GlobalNamespace.CollectTypeSymbols(targetSymbol)
            .Where(x => !x.IsAbstract)
            .Where(x => !x.HasAttribute<IgnoreAttribute>())
            .OrderBy(x => x.ToDisplayString());

        var source = new SourceStringBuilder(symbol);

        source.Line();
        source.Line("using System;");
        source.Line("using System.Collections.Generic;");
        source.Line("using Avalonia.Controls;");
        source.Line("using Avalonia.Controls.Templates;");
        source.Line();

        source.PartialTypeBlockBrace(
            "IDataTemplate",
            () =>
            {
                source.Line(
                    "public static Dictionary<Type, Func<object, Control>> ViewMap { get; } = new()"
                );
                source.BlockDecl(() =>
                {
                    foreach (var viewModelSymbol in viewModelSymbols)
                    {
                        var view = GetView(viewModelSymbol, compilation);

                        if (view is null)
                            continue;

                        source.Line(
                            $"[typeof({viewModelSymbol.ToFullDisplayString()})] = (vm) => new {view.ToFullDisplayString()}() {{ ViewModel = ({viewModelSymbol.ToDisplayString()})vm }},"
                        );
                    }
                });
            }
        );

        return (
            source.ToString(),
            new DiagnosticDetail(nameof(StaticViewLocatorGenerator), "Successful Generation")
        );
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
}
