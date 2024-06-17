using System.Collections.Generic;
using Avayomi.Generators.Abstractions;

namespace Avayomi.Generators.Statics;

internal class DependencyInjectionStatic : StaticGenerator
{
    private readonly string _serviceCollectionExtensionsText = $$"""
        using Microsoft.Extensions.DependencyInjection;

        namespace {{Namespace()}}
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
        """;

    public override IEnumerable<(string FileName, string Source)> Generate()
    {
        yield return (
            $"{MetadataNames.DependencyInjectionNamespace}.g.cs",
            _serviceCollectionExtensionsText
        );
    }

    private static string Namespace(params string[] parts)
    {
        var temp = new List<string> { MetadataNames.Namespace };
        temp.AddRange(parts);
        return $"{string.Join(".", temp)}";
    }
}
