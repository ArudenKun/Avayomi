using System.Collections.Immutable;
using System.Threading;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Avayomi.SourceGenerator.AvaloniaResource;

[Generator]
internal sealed class Generator : IIncrementalGenerator
{
    private static readonly string AssemblyName = typeof(Generator).Assembly.GetName().Name;

    public const string Name = "AvaloniaResourcesGenerator";
    public const string Id = "ARG";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context
            .AdditionalTextsProvider.Combine(context.AnalyzerConfigOptionsProvider)
            .Where(static pair =>
                pair.Right.GetOption(pair.Left, "Resource", prefix: Name)?.ToUpperInvariant()
                == "TRUE"
            )
            .Select(static (x, _) => new Resource(x.Left.Path))
            .Collect()
            .Combine(context.AnalyzerConfigOptionsProvider)
            .SelectAndReportExceptions(GetSourceCode, context, Id)
            .AddSource(context);
    }

    private static EquatableArray<FileWithName> GetSourceCode(
        (ImmutableArray<Resource> Resources, AnalyzerConfigOptionsProvider Options) tuple,
        CancellationToken cancellationToken = default
    )
    {
        var (resources, options) = tuple;
        var @namespace =
            options.GetGlobalOption("Namespace", prefix: Name)
            ?? (
                options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var value)
                    ? value
                    : string.Empty
            );
        var modifier = options.GetGlobalOption("Modifier", prefix: Name) ?? "internal";
        var className = options.GetGlobalOption("ClassName", prefix: Name) ?? "AvaloniaResources";

        return new[]
        {
            new FileWithName(
                Name: $"{AssemblyName}.AvaloniaResource.g.cs",
                Text: CodeGenerator.GenerateResource(@namespace: @namespace, modifier: modifier)
            ),
            new FileWithName(
                Name: $"{AssemblyName}.AvaloniaResources.g.cs",
                Text: CodeGenerator.GenerateResources(
                    @namespace: @namespace,
                    modifier: modifier,
                    className: className,
                    resources: resources
                )
            ),
        }
            .ToImmutableArray()
            .AsEquatableArray();
    }
}
