using System.Collections.Immutable;
using System.Linq;
using Generator.Attributes;
using Generator.Extensions;
using Generator.Models;
using Generator.Utilities;
using H;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;

namespace Generator.Generators;

[Generator]
internal sealed class AvaloniaPropertyGenerator : IIncrementalGenerator
{
    private const string Id = "APG";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(
                $"{typeof(AvaloniaPropertyAttribute).FullName}.g.cs",
                Resources.AvaloniaPropertyAttribute_cs.AsString()
            );
        });

        var nonGeneric = context.SyntaxProvider.ForAttributeWithMetadataNameOfClassesAndRecords(
            $"{typeof(AvaloniaPropertyAttribute).FullName}"
        );

        var generic = context.SyntaxProvider.ForAttributeWithMetadataNameOfClassesAndRecords(
            $"{typeof(AvaloniaPropertyAttribute<>).FullName}"
        );

        var staticConstructorSyntaxProvider = context
            .SyntaxProvider.ForAttributeWithMetadataNameOfClassesAndRecords(
                $"{typeof(AvaloniaPropertyAttribute).FullName}"
            )
            .SelectManyAllAttributesOfCurrentClassSyntax()
            .SelectAndReportExceptions(PrepareData, context, Id)
            .WhereNotNull()
            .CollectAsEquatableArray();

        context
            .CompilationProvider.Combine(staticConstructorSyntaxProvider)
            .SelectAndReportExceptions(x => GetSourceCode(x.Left, x.Right), context, Id)
            .AddSource(context);

        context.SyntaxProvider.ForAttributeWithMetadataNameOfClassesAndRecords(
            $"{typeof(AvaloniaPropertyAttribute<>).FullName}"
        );

        var combinedCompilationProvider = context
            .CompilationProvider.Combine(nonGeneric.Collect())
            .Combine(generic.Collect());

        context.RegisterImplementationSourceOutput(
            combinedCompilationProvider,
            (sourceProductionContext, provider) =>
                OnExecute(
                    sourceProductionContext,
                    provider.Left.Left,
                    provider.Left.Right,
                    provider.Right
                )
        );
    }

    private static (ClassData Class, AvaloniaPropertyData AvaloniaProperty)? PrepareData(
        ClassWithAttributesContext context
    )
    {
        var (_, attributes, _, classSymbol) = context;
        if (attributes.FirstOrDefault() is not { } attribute)
        {
            return null;
        }

        var classData = classSymbol.GetClassData();
        var dependencyPropertyData = attribute.GetAvaloniaPropertyData();

        return (classData, dependencyPropertyData);
    }

    private static FileWithName GetSourceCode(
        Compilation compilation,
        EquatableArray<(ClassData Class, AvaloniaPropertyData AvaloniaProperty)> values
    )
    {
        if (values.AsImmutableArray().IsDefaultOrEmpty)
        {
            return FileWithName.Empty;
        }

        var @class = values.First().Class;
        var classSymbol = @class.TypeSymbol;
        var avaloniaProperties = values.Select(static x => x.AvaloniaProperty).ToArray();
        if (!avaloniaProperties.Where(static property => !property.IsDirect).Any())
            return FileWithName.Empty;
        {
            var source = new SourceStringBuilder(classSymbol);

            var genericStyledPropertySymbol = compilation.GetTypeByMetadataName(
                "Avalonia.StyledProperty`1"
            );

            var avaloniaPropertySymbol = compilation.GetTypeByMetadataName(
                "Avalonia.AvaloniaProperty"
            );

            source.Line("#nullable enable");

            source.PartialTypeBlockBrace(() =>
            {
                source.StaticConstructor(() =>
                {
                    foreach (var avaloniaProperty in avaloniaProperties)
                    {
                        if (genericStyledPropertySymbol is null || avaloniaPropertySymbol is null)
                            continue;

                        var propertyName = avaloniaProperty.Name + "Property";

                        INamedTypeSymbol targetTypeSymbol;
                        if (avaloniaProperty.TypeSymbol.IsValueType)
                            targetTypeSymbol = avaloniaProperty.TypeSymbol;
                        else
                            targetTypeSymbol = compilation
                                .GetSpecialType(SpecialType.System_Nullable_T)
                                .Construct(avaloniaProperty.TypeSymbol);

                        var type = targetTypeSymbol.ToDisplayString();

                        var onChanged1 = classSymbol.IsPartialMethodImplemented(
                            $"On{avaloniaProperty.Name}Changed()"
                        );

                        var onChanged2 = classSymbol.IsPartialMethodImplemented(
                            $"On{avaloniaProperty.Name}Changed({type})"
                        );

                        var onChanged3 = classSymbol.IsPartialMethodImplemented(
                            $"On{avaloniaProperty.Name}Changed({type}, {type})"
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
                                        $"(({classSymbol.ToFullDisplayString()})x.Sender).On{avaloniaProperty.Name}Changed();"
                                    );

                                if (onChanged2)
                                    source.Line(
                                        $"(({classSymbol.ToFullDisplayString()})x.Sender).On{avaloniaProperty.Name}Changed(({targetTypeSymbol.ToFullDisplayString()})x.NewValue.GetValueOrDefault());"
                                    );

                                if (onChanged3)
                                    source.Line(
                                        $"(({classSymbol.ToFullDisplayString()})x.Sender).On{avaloniaProperty.Name}Changed(({targetTypeSymbol.ToFullDisplayString()})x.OldValue.GetValueOrDefault(), ({targetTypeSymbol.ToFullDisplayString()})x.NewValue.GetValueOrDefault());"
                                    );
                            },
                            "));"
                        );
                    }
                });
            });

            var text = source.ToString();

            if (!string.IsNullOrWhiteSpace(text))
            {
                return new FileWithName(
                    Name: $"{@class.FullName}.StaticConstructor.g.cs",
                    Text: text
                );
            }
        }

        return FileWithName.Empty;
    }

    private static void OnExecute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<GeneratorAttributeSyntaxContext> nonGenerics,
        ImmutableArray<GeneratorAttributeSyntaxContext> generics
    )
    {
        GenerateCode(context, compilation, nonGenerics, false);
        GenerateCode(context, compilation, generics, true);
    }

    private static void GenerateCode(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<GeneratorAttributeSyntaxContext> syntaxContexts,
        bool isGeneric
    )
    {
        foreach (var syntaxContext in syntaxContexts)
        {
            var classSymbol = (INamedTypeSymbol)syntaxContext.TargetSymbol;

            var attributeDatas = syntaxContext.Attributes;

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
                foreach (
                    var attribute in attributeDatas.Select(attributeData =>
                        attributeData.GetAvaloniaPropertyData()
                    )
                )
                {
                    if (genericStyledPropertySymbol is null || avaloniaPropertySymbol is null)
                        continue;

                    ITypeSymbol targetTypeSymbol;
                    if (attribute.IsValueType)
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

                    foreach (var propertyAttribute in attribute.PropertyAttributes)
                        source.Line($"[{propertyAttribute}]");
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

            var hintName = $"{classSymbol.ToDisplayString()}.AvaloniaProperty";
            if (isGeneric)
            {
                hintName += "`1";
            }

            context.AddSource($"{hintName}.g.cs", source.ToString());
        }
    }
}
