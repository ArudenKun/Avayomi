using System;
using System.Linq;
using Avayomi.Generators.Abstractions;
using Avayomi.Generators.Attributes;
using Avayomi.Generators.Utilities;
using Microsoft.CodeAnalysis;

namespace Avayomi.Generators.Steps;

[Generator]
internal sealed class StaticViewLocatorStep
    : GeneratorStepForDeclaredTypeWithAttribute<StaticViewLocatorAttribute>
{
    protected override string Execute(
        INamedTypeSymbol classSymbol,
        AttributeData attributeData,
        SourceStringBuilder source
    )
    {
        var targetSymbol = Context.Compilation.GetTypeByMetadataName(
            MetadataNames.ObservableObject
        );

        var viewModelSymbols = Context
            .Compilation.GlobalNamespace.CollectTypeSymbols(targetSymbol)
            .OrderBy(x => x.ToDisplayString());

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
                    "public static Dictionary<Type, Func<Control>> ViewMap { get; } = new()"
                );
                source.BlockDecl(() =>
                {
                    foreach (var viewModelSymbol in viewModelSymbols)
                    {
                        var view = GetView(viewModelSymbol);

                        if (view is null)
                        {
                            continue;
                        }

                        source.Line(
                            $"[typeof({viewModelSymbol.ToFullDisplayString()})] = () => new {view.ToFullDisplayString()}(),"
                        );
                    }
                });
            }
        );

        return source.ToString();
    }

    private INamedTypeSymbol GetView(ISymbol symbol)
    {
        var viewName = symbol.ToDisplayString().Replace("ViewModel", "View");

        var viewSymbol = Context.Compilation.GetTypeByMetadataName(viewName);

        if (viewSymbol is not null)
        {
            return viewSymbol;
        }

        viewName = symbol.ToDisplayString().Replace(".ViewModels.", ".Views.");
        viewName = viewName.Remove(viewName.IndexOf("ViewModel", StringComparison.Ordinal));
        return Context.Compilation.GetTypeByMetadataName(viewName);
    }
}
