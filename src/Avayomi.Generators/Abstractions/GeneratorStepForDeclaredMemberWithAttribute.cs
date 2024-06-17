using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Avayomi.Generators.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Avayomi.Generators.Abstractions;

internal abstract class GeneratorStepForDeclaredMemberWithAttribute<TAttribute, TDeclarationSyntax>
    : GeneratorStep<TDeclarationSyntax>
    where TAttribute : Attribute
    where TDeclarationSyntax : MemberDeclarationSyntax
{
    protected static readonly string AttributeType = typeof(TAttribute).Name;

    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    protected static readonly string AttributeName = Regex.Replace(
        AttributeType,
        "Attribute$",
        "",
        RegexOptions.Compiled
    );

    public override bool Filter(TDeclarationSyntax node) => HasAttributeType(node);

    protected bool HasAttributeType(MemberDeclarationSyntax type)
    {
        if (type.AttributeLists.Count is 0)
            return false;

        return type
            .AttributeLists.SelectMany(attributeList => attributeList.Attributes)
            .Any(attribute => attribute.Name.ToString() == AttributeName);
    }

    protected string GenerateFilename(ISymbol symbol)
    {
        var gn = $"{Format(symbol)}.{AttributeName}{Ext}";
        // Log.Debug($"Generated Filename ({gn.Length}): {gn}\n");
        return gn;

        static string Format(ISymbol symbol) =>
            string.Join("_", $"{symbol}".Split(InvalidFileNameChars))
                .Truncate(MaxFileLength - Ext.Length);
    }

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
}
