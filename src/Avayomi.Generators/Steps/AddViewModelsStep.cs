using System.Linq;
using Avayomi.Generators.Abstractions;
using Avayomi.Generators.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Avayomi.Generators.Steps;

internal class AddViewModelsStep : GeneratorStep<ClassDeclarationSyntax>
{
    public override void Execute(ClassDeclarationSyntax[] declarationSyntaxes)
    {
        var targetSymbol = Context.Compilation.GetTypeByMetadataName(
            MetadataNames.ObservableObject
        );

        var viewModels = GetAll<INamedTypeSymbol>(declarationSyntaxes)
            .Where(x => !x.IsAbstract)
            .Where(x => x.Name.EndsWith("ViewModel"))
            .Where(x => x.IsOfBaseType(targetSymbol))
            .OrderBy(x => x.ToDisplayString())
            .ToArray();

        var source = new SourceStringBuilder();

        source.Line();
        source.Line("using Microsoft.Extensions.DependencyInjection;");
        source.Line();

        source.NamespaceBlockBrace(
            $"{MetadataNames.Namespace}",
            () =>
            {
                source.Line("public static partial class ServiceCollectionExtensions");
                source.BlockBrace(() =>
                {
                    source.Line("static partial void AddViewModels(IServiceCollection services)");
                    source.BlockBrace(() =>
                    {
                        foreach (var viewModel in viewModels)
                        {
                            source.Line(
                                viewModel.HasAttribute("SingletonAttribute")
                                    ? $"services.AddSingleton<{viewModel.ToFullDisplayString()}>();"
                                    : $"services.AddTransient<{viewModel.ToFullDisplayString()}>();"
                            );

                            if (viewModel.BaseType is not { } viewModelBaseType)
                                continue;

                            source.Line(
                                viewModel.HasAttribute("SingletonAttribute")
                                    ? $"services.AddSingleton<{viewModelBaseType.ToFullDisplayString()}>(sp => sp.GetRequiredService<{viewModel.ToFullDisplayString()}>());"
                                    : $"services.AddTransient<{viewModelBaseType.ToFullDisplayString()}>(sp => sp.GetRequiredService<{viewModel.ToFullDisplayString()}>());"
                            );
                        }
                    });
                });
            }
        );

        Context.Context.AddSource(
            $"{MetadataNames.DependencyInjectionNamespace}.AddViewModels.g.cs",
            source.ToString()
        );
    }
}
