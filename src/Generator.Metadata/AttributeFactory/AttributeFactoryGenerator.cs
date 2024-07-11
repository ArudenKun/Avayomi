﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.Metadata.AttributeFactory;

[Generator(LanguageNames.CSharp)]
internal sealed partial class AttributeFactoryGenerator : IIncrementalGenerator
{
    //source: https://stackoverflow.com/a/58853591
    private static readonly Regex _camelCasePattern =
        new(@"([A-Z])([A-Z]+|[a-z0-9_]+)($|[A-Z]\w*)", RegexOptions.Compiled);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targetSet = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        var provider = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                _generateFactoryAttributeFullyQualifiedName,
                static (n, t) =>
                    n is TypeDeclarationSyntax tds && tds.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (c, t) => AttributeSourceModel.Create(c, t)
            )
            .Where(m => targetSet.Add(m.Symbol))
            .Select(
                static (m, t) =>
                {
                    var typeProps = m
                        .Symbol.GetMembers()
                        .OfType<IPropertySymbol>()
                        .Where(static p =>
                            p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            == "global::System.Type"
                        )
                        .ToArray();

                    var containerFields = typeProps
                        .Select(static p => ToCamelCase(p.Name))
                        .Select(static n => $"private Object? _{n}SymbolContainer;");

                    var symbolProps = typeProps.Select(static p =>
                        $"\t\tpublic INamedTypeSymbol{(p.NullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty)} {p.Name}Symbol{{"
                        + $"get => (INamedTypeSymbol)_{ToCamelCase(p.Name)}SymbolContainer;}}"
                    );

                    var propCases = m
                        .Symbol.GetMembers()
                        .OfType<IPropertySymbol>()
                        .Where(static p =>
                            p.SetMethod != null
                            && p.SetMethod.DeclaredAccessibility == Accessibility.Public
                            && !p.SetMethod.IsInitOnly
                        )
                        .Select(static p =>
                            (
                                Type: p.Type.ToDisplayString(
                                    SymbolDisplayFormat.FullyQualifiedFormat
                                ),
                                p.Name
                            )
                        )
                        .Select(static t =>
                            (
                                t.Type,
                                t.Name,
                                IsArray: t.Type.EndsWith("[]"),
                                IsObjectArray: t.Type == "object[]",
                                IsType: t.Type == "global::System.Type"
                            )
                        )
                        .Select(static t =>
                            (
                                t.Name,
                                t.IsType,
                                t.Type,
                                Expression: t.IsArray
                                    ? t.IsObjectArray
                                        ?
                                        //object array
                                        "getValues(propArg.Value)"
                                        :
                                        //regular array
                                        $"propArg.Value.Values.Select(c => ({t.Type[..^2]})c.Value).ToArray()"
                                    :
                                    //scalar
                                    $"{(t.IsType || t.Type == "object" ? string.Empty : $"({t.Type})")}propArg.Value.Value"
                            )
                        )
                        .Select(static t =>
                            $"case \"{t.Name}\" : try{{result.{(t.IsType ? $"_{ToCamelCase(t.Name)}SymbolContainer" : t.Name)} = {t.Expression};}}catch{{}}break;"
                        );

                    var source = m
                        .Source.Replace(_targetPropCasesPlaceholder, string.Join("\n", propCases))
                        .Replace(_targetSymbolsPlaceholder, string.Join("\n", symbolProps))
                        .Replace(_targetContainersPlaceholder, string.Join("\n", containerFields));

                    return m.WithSource(source);
                }
            )
            .Select(
                static (m, t) =>
                {
                    var ctorCases = m
                        .Symbol.Constructors.Where(static c =>
                            !c.GetAttributes()
                                .Select(static a => a.AttributeClass)
                                .Any(static c =>
                                    c != null
                                    && c.Name == _excludeConstructorAttributeName
                                    && c.ContainingNamespace.ToDisplayString()
                                        == _attributeNamespace
                                )
                        )
                        .GroupBy(static c => c.Parameters.Length)
                        .Select(static g =>
                        {
                            var groupBranches = g.Select(static c =>
                                {
                                    var parameters = c
                                        .Parameters.Select(static p =>
                                            (
                                                NullableType: GetNullableTypeName(p.Type),
                                                NonNullableType: GetNonNullableTypeName(p.Type),
                                                p.Name,
                                                parameter: p,
                                                HasDefault: p.HasExplicitDefaultValue
                                            )
                                        )
                                        .Select(static t =>
                                            (
                                                t.HasDefault,
                                                Default: GetDefaultLiteral(
                                                    t.NullableType,
                                                    t.NonNullableType,
                                                    t.parameter
                                                ),
                                                t.NullableType,
                                                t.NonNullableType,
                                                t.Name,
                                                IsArray: t.NullableType.EndsWith("[]"),
                                                IsObjectArray: t.NullableType == "object[]",
                                                IsType: t.NullableType == "global::System.Type"
                                            )
                                        )
                                        .ToArray();

                                    var conditions = string.Join(
                                        "&&\n",
                                        parameters.Select(
                                            static (p, i) =>
                                                $"(ctorArgs[{i}].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions((SymbolDisplayGenericsOptions)5)) == \"{p.NullableType}\" ||"
                                                + $"{(p.HasDefault ? "true" : "false")})"
                                        )
                                    );
                                    var body = string.Concat(
                                        parameters.Select(
                                            static (p, i) =>
                                            {
                                                var argDeclaration = p.IsArray
                                                    ? p.IsObjectArray
                                                        ?
                                                        //object array
                                                        $"var arg{i} = getValues(ctorArgs[{i}]);"
                                                        :
                                                        //regular array
                                                        $"var arg{i} = ctorArgs[{i}].Values.Select(c => ({p.NullableType[..^2]})c.Value).ToArray();"
                                                    :
                                                    //scalar
                                                    $"var arg{i} = ctorArgs[{i}].IsNull ? default : {(p.HasDefault ? $"ctorArgs[{i}].Value != null ? " : "")}{(p.IsType || p.NullableType == "object" ? string.Empty : $"({p.NonNullableType})")}ctorArgs[{i}].Value{(p.HasDefault ? $" : {p.Default}" : "")};";

                                                return argDeclaration;
                                            }
                                        )
                                    );

                                    var args = string.Join(
                                        ",",
                                        parameters.Select(
                                            (p, i) =>
                                                $"{(p.IsType ? $"{p.Name}SymbolContainer" : p.Name)}:arg{i}"
                                        )
                                    );
                                    var result =
                                        $"{(c.Parameters.Length > 0 ? $"if({conditions}){{" : string.Empty)}{body}result = new {{NAME}}{{GENERICPARAMLIST}}({args});{(c.Parameters.Length > 0 ? "}" : string.Empty)}";

                                    return result;
                                })
                                .ToList();
                            if (g.Key != 0)
                                groupBranches.Add("{return false;}break;");
                            else
                                groupBranches[0] = groupBranches[0] + "break;";

                            var groupBranchesSource =
                                $"case {g.Key}:{string.Join("else ", groupBranches)}";

                            return groupBranchesSource;
                        })
                        .Append("default:return false;");

                    var ctorCasesSource = string.Concat(ctorCases);

                    var source = m.Source.Replace(_targetCtorCasesPlaceholder, ctorCasesSource);

                    return m.WithSource(source);
                }
            )
            .Select(
                static (m, t) =>
                {
                    var metadataName = GetFullMetadataName(m.Symbol);
                    var genericParamList =
                        m.Symbol.TypeParameters.Length > 0
                            ? $"<{string.Join(", ", m.Symbol.TypeParameters.Select(p => p.Name))}>"
                            : string.Empty;
                    var genericParamComment =
                        m.Symbol.TypeParameters.Length > 0
                            ? $"{{{string.Join(", ", m.Symbol.TypeParameters.Select(p => p.Name))}}}"
                            : string.Empty;
                    var genericParamNames =
                        m.Symbol.TypeParameters.Length > 0
                            ? $"_Of_{string.Join("_And_", m.Symbol.TypeParameters.Select(p => p.Name))}"
                            : string.Empty;

                    var source = m
                        .Source.Replace(_targetMetadataNamePlaceholder, metadataName)
                        .Replace(_genericParamLisTMacro, genericParamList)
                        .Replace(_genericParamNamesPlaceholder, genericParamNames)
                        .Replace(_genericParamCommenTMacro, genericParamComment)
                        .Replace(_targetNamePlaceholder, m.Symbol.Name)
                        .Replace(
                            _targetAccessibilityPlaceholder,
                            SyntaxFacts.GetText(m.Symbol.DeclaredAccessibility)
                        )
                        .Replace(
                            _targetNamespacePlaceholder,
                            m.Symbol.ContainingNamespace.ToDisplayString()
                        );

                    return m.WithSource(source);
                }
            )
            .Select(
                static (m, t) =>
                {
                    var hintNames = m
                        .Symbol.ToDisplayString(
                            SymbolDisplayFormat
                                .FullyQualifiedFormat.WithMiscellaneousOptions(
                                    /*
                                        get rid of special types

                                                10110
                                        NAND 00100
                                            => 10010

                                                10110
                                            &! 00100
                                            => 10010

                                                00100
                                            ^ 11111
                                            => 11011

                                                10110
                                            & 11011
                                            => 10010
                                    */
                                    SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
                                        & (
                                            SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                                            ^ (SymbolDisplayMiscellaneousOptions)int.MaxValue
                                        )
                                )
                                .WithGenericsOptions(
                                    SymbolDisplayGenericsOptions.IncludeTypeParameters
                                )
                        )
                        .Replace("<", "_of_")
                        .Replace('>', '_')
                        .Replace(",", "_and_")
                        .Replace(" ", string.Empty)
                        .Replace('.', '_')
                        .Replace("::", "_")
                        .TrimEnd('_');

                    var hintName = $"{hintNames}.g.cs";

                    //source: https://stackoverflow.com/a/74412674
                    var source = CSharpSyntaxTree
                        .ParseText(m.Source, cancellationToken: t)
                        .GetRoot(t)
                        .NormalizeWhitespace()
                        .SyntaxTree.GetText(t)
                        .ToString();

                    return (Hint: hintName, Source: source);
                }
            );

        context.RegisterPostInitializationOutput(static c =>
            c.AddSource(_attributesHint, _attributeSource)
        );
        context.RegisterSourceOutput(provider, static (c, s) => c.AddSource(s.Hint, s.Source));
    }

    private static string ToCamelCase(string name)
    {
        if (name.Length == 0)
            return name;

        if (name.Length == 1 && char.IsLower(name[0]))
            return name;

        //source: https://stackoverflow.com/a/58853591
        var result = _camelCasePattern.Replace(
            name,
            m => m.Groups[1].Value.ToLower() + m.Groups[2].Value.ToLower() + m.Groups[3].Value
        );

        return result;
    }

    //source: https://stackoverflow.com/a/27106959
    private static string GetFullMetadataName(ISymbol s)
    {
        if (s == null || isRootNamespace(s))
            return string.Empty;

        var sb = new StringBuilder(s.MetadataName);
        var last = s;

        s = s.ContainingSymbol;

        while (!isRootNamespace(s))
        {
            _ = s is ITypeSymbol && last is ITypeSymbol ? sb.Insert(0, '+') : sb.Insert(0, '.');

            //_ = sb.Insert(0, s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            _ = sb.Insert(0, s.MetadataName);
            s = s.ContainingSymbol;
        }

        return sb.ToString();

        static bool isRootNamespace(ISymbol symbol)
        {
            return symbol is INamespaceSymbol s && s.IsGlobalNamespace;
        }
    }

    private static string GetNonNullableTypeName(ITypeSymbol type)
    {
        var result = GetNullableTypeName(type).Replace("?", "");

        return result;
    }

    private static string GetNullableTypeName(ITypeSymbol type)
    {
        var result = type.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions(
                (SymbolDisplayGenericsOptions)5
            )
        );

        return result;
    }

    private static string GetDefaultLiteral(
        string nullable,
        string nonNullable,
        IParameterSymbol parameter
    )
    {
        return parameter.HasExplicitDefaultValue
            ? parameter.ExplicitDefaultValue switch
            {
                char => $"'{parameter.ExplicitDefaultValue}'",
                string => $"\"{parameter.ExplicitDefaultValue}\"",
                true => "true",
                false => "false",
                _
                    => parameter.ExplicitDefaultValue == null
                        ? $"default({nullable})"
                        : $"({nonNullable}){parameter.ExplicitDefaultValue}"
            }
            : string.Empty;
    }
}
