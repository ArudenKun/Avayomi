using System.Collections.Immutable;
using Generator.Attributes;
using Generator.Extensions;
using Generator.Utilities;
using Microsoft.CodeAnalysis;

namespace Generator.Generators;

[Generator]
internal sealed class AvaloniaPropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.ForAttributeWithMetadataNameOfClassesAndRecords(
            $"{typeof(AvaloniaPropertyAttribute).FullName}"
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
        ImmutableArray<GeneratorAttributeSyntaxContext> syntaxContexts
    )
    {
        foreach (var syntaxContext in syntaxContexts)
        {
            var classSymbol = (INamedTypeSymbol)syntaxContext.TargetSymbol;

            var attributeDatas = classSymbol.GetAttributes();

            var genericStyledPropertySymbol = compilation.GetTypeByMetadataName(
                "Avalonia.StyledProperty`1"
            );

            var avaloniaPropertySymbol = compilation.GetTypeByMetadataName(
                "Avalonia.AvaloniaProperty"
            );

            var source = new SourceStringBuilder(classSymbol);

            source.Line("#nullable enable");

            source.Line();
            source.PartialTypeBlockBrace(() =>
            {
                source.StaticConstructor(() =>
                {
                    foreach (var attributeData in attributeDatas)
                    {
                        var attribute = AvaloniaPropertyAttribute.TryCreate(
                            attributeData,
                            out var result
                        )
                            ? result
                            : null;

                        if (
                            attribute is null
                            || genericStyledPropertySymbol is null
                            || avaloniaPropertySymbol is null
                        )
                            continue;

                        var propertyName = attribute.Name + "Property";

                        INamedTypeSymbol targetTypeSymbol;
                        if (attribute.TypeSymbol.IsValueType)
                            targetTypeSymbol = attribute.TypeSymbol;
                        else
                            targetTypeSymbol = compilation
                                .GetSpecialType(SpecialType.System_Nullable_T)
                                .Construct(attribute.TypeSymbol);

                        var type = targetTypeSymbol.ToDisplayString();

                        var onChanged1 = classSymbol.IsPartialMethodImplemented(
                            $"On{attribute.Name}Changed()"
                        );

                        var onChanged2 = classSymbol.IsPartialMethodImplemented(
                            $"On{attribute.Name}Changed({type})"
                        );

                        var onChanged3 = classSymbol.IsPartialMethodImplemented(
                            $"On{attribute.Name}Changed({type}, {type})"
                        );

                        if (!onChanged1 && !onChanged2 && !onChanged3)
                            continue;

                        source.Line(
                            $"{propertyName}.Changed.Subscribe(new global::Avalonia.Reactive.AnonymousObserver<global::Avalonia.AvaloniaPropertyChangedEventArgs<{targetTypeSymbol.ToFullDisplayString()}>>(static x =>"
                        );
                        source.BlockDecl(
                            () =>
                            {
                                if (onChanged1)
                                    source.Line(
                                        $"(({classSymbol.ToFullDisplayString()})x.Sender).On{attribute.Name}Changed();"
                                    );

                                if (onChanged2)
                                    source.Line(
                                        $"(({classSymbol.ToFullDisplayString()})x.Sender).On{attribute.Name}Changed(({targetTypeSymbol.ToFullDisplayString()})x.NewValue.GetValueOrDefault());"
                                    );

                                if (onChanged3)
                                    source.Line(
                                        $"(({classSymbol.ToFullDisplayString()})x.Sender).On{attribute.Name}Changed(({targetTypeSymbol.ToFullDisplayString()})x.OldValue.GetValueOrDefault(), ({targetTypeSymbol.ToFullDisplayString()})x.NewValue.GetValueOrDefault());"
                                    );
                            },
                            "));"
                        );
                        source.Line();
                    }
                });

                source.Line();

                foreach (var attributeData in attributeDatas)
                {
                    var attribute = AvaloniaPropertyAttribute.TryCreate(
                        attributeData,
                        out var result
                    )
                        ? result
                        : null;

                    if (
                        attribute is null
                        || genericStyledPropertySymbol is null
                        || avaloniaPropertySymbol is null
                    )
                        continue;

                    INamedTypeSymbol targetTypeSymbol;
                    if (attribute.TypeSymbol.IsValueType)
                        targetTypeSymbol = attribute.TypeSymbol;
                    else
                        targetTypeSymbol = compilation
                            .GetSpecialType(SpecialType.System_Nullable_T)
                            .Construct(attribute.TypeSymbol);

                    var styledPropertySymbol = genericStyledPropertySymbol.Construct(
                        targetTypeSymbol
                    );

                    source.Line(
                        $"public static readonly {styledPropertySymbol.ToFullDisplayString()} {attribute.Name}Property ="
                    );
                    source.BlockTab(() =>
                    {
                        source.Line(
                            $"{avaloniaPropertySymbol.ToFullDisplayString()}.Register<{classSymbol.ToFullDisplayString()}, {targetTypeSymbol.ToFullDisplayString()}>("
                        );
                        source.BlockTab(() =>
                        {
                            source.Line($"name: \"{attribute.Name}\"");
                        });
                    });
                    source.BlockTab(() => source.Line(");"));
                    source.Line();

                    var propertyAttributes = attributeData.GetArgumentArray<INamedTypeSymbol>(
                        nameof(Attributes)
                    );

                    foreach (var propertyAttribute in propertyAttributes)
                        source.Line($"[{propertyAttribute.ToFullDisplayString()}]");
                    source.Line(
                        $"public {targetTypeSymbol.ToFullDisplayString()} {attribute.Name}"
                    );
                    source.BlockBrace(() =>
                    {
                        source.Line($"get => GetValue({attribute.Name}Property);");
                        source.Line($"set => SetValue({attribute.Name}Property, value);");
                    });

                    source.Line();
                    source.Line($"partial void On{attribute.Name}Changed();");
                    source.Line(
                        $"partial void On{attribute.Name}Changed({targetTypeSymbol.ToFullDisplayString()} newValue);"
                    );
                    source.Line(
                        $"partial void On{attribute.Name}Changed({targetTypeSymbol.ToFullDisplayString()} oldValue, {targetTypeSymbol.ToFullDisplayString()} newValue);"
                    );
                    source.Line();
                }
            });

            context.AddSource(
                $"{classSymbol.ToDisplayString()}.AvaloniaProperty.g.cs",
                source.ToString()
            );
        }
    }
}
