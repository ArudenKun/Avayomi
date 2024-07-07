using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Generator.Attributes;
using Generator.Extensions;
using Generator.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Generator.Generators;

[Generator]
internal sealed class DependencyInjectionGenerator
    : SourceGeneratorForDeclaredMember<ClassDeclarationSyntax>
{
    protected override IEnumerable<(string Name, string Source)> StaticSources { get; } =
        [
            (
                $"{MetadataNames.DependencyInjectionNamespace}.statics.g.cs",
                $$"""
                using Microsoft.Extensions.DependencyInjection;

                namespace {{MetadataNames.DependencyInjectionNamespace}}
                {
                    public static partial class ServiceCollectionExtensions
                    {
                        static partial void AddViewModels(IServiceCollection services);
                    
                        public static IServiceCollection AddCore(this IServiceCollection services)
                        {
                            AddViewModels(services);
                            return services;
                        }
                    }
                }
                """
            )
        ];

    protected override (string FileName, string GeneratedCode) GenerateCode(
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> nodes,
        AnalyzerConfigOptions options
    )
    {
        var targetSymbol = compilation.GetTypeByMetadataName(MetadataNames.ObservableObject);

        var viewModels = GetAll<INamedTypeSymbol>(nodes)
            .Where(x => !x.IsAbstract)
            .Where(x => !x.HasAttribute<IgnoreAttribute>())
            .Where(x => x.Name.EndsWith("ViewModel"))
            .Where(x => x.IsOfBaseType(targetSymbol!))
            .OrderBy(x => x.ToDisplayString())
            .ToArray();

        var source = new SourceStringBuilder();

        source.Line();
        source.Line("using Microsoft.Extensions.DependencyInjection;");
        source.Line();

        source.NamespaceBlockBrace(
            $"{MetadataNames.DependencyInjectionNamespace}",
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
                                viewModel.HasAttribute<SingletonAttribute>()
                                    ? $"services.AddSingleton<{viewModel.ToFullDisplayString()}>();"
                                    : $"services.AddTransient<{viewModel.ToFullDisplayString()}>();"
                            );

                            if (viewModel.BaseType is not { } viewModelBaseType)
                                continue;

                            source.Line(
                                viewModel.HasAttribute<SingletonAttribute>()
                                    ? $"services.AddSingleton<{viewModelBaseType.ToFullDisplayString()}>(sp => sp.GetRequiredService<{viewModel.ToFullDisplayString()}>());"
                                    : $"services.AddTransient<{viewModelBaseType.ToFullDisplayString()}>(sp => sp.GetRequiredService<{viewModel.ToFullDisplayString()}>());"
                            );
                        }
                    });
                });
            }
        );

        return ($"{MetadataNames.DependencyInjectionNamespace}.g.cs", source.ToString());
    }
}
