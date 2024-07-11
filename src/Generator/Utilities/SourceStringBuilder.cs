﻿using System;
using System.Text;
using Generator.Extensions;
using Microsoft.CodeAnalysis;

namespace Generator.Utilities;

internal sealed class SourceStringBuilder
{
    private const string TopMessage = """
        //------------------------------------------------------------------------------
        // <auto-generated>
        //     This code was generated.
        //
        //     Changes to this file may cause incorrect behavior and will be lost if
        //     the code is regenerated.
        // </auto-generated>
        //------------------------------------------------------------------------------
        """;

    private const string BottomMessage = """
        //------------------------------------------------------------------------------
        // <auto-generated>
        //     Roslyn doesn't clear the file when writing debug output for
        //     EmitCompilerGeneratedFiles, so I'm writing this message to
        //     make it more obvious what's going on when that happens.
        // </auto-generated>
        //------------------------------------------------------------------------------
        """;

    private readonly StringBuilder _indentPrefix = new();
    private readonly StringBuilder _sourceBuilder = new();
    private readonly ITypeSymbol _typeSymbol = null!;

    public SourceStringBuilder(ITypeSymbol typeSymbol)
    {
        _sourceBuilder.Append(TopMessage);
        _sourceBuilder.AppendLine();
        _typeSymbol = typeSymbol;
    }

    public SourceStringBuilder(IMethodSymbol methodSymbol)
    {
        _sourceBuilder.Append(TopMessage);
        _sourceBuilder.AppendLine();
        _typeSymbol = methodSymbol.ContainingType;
    }

    public SourceStringBuilder()
    {
        _sourceBuilder.Append(TopMessage);
        _sourceBuilder.AppendLine();
    }

    public void Raw(string rawText)
    {
        _sourceBuilder.Append(rawText);
    }

    public void Line(params string[] parts)
    {
        if (parts.Length != 0)
        {
            _sourceBuilder.Append(_indentPrefix);

            foreach (var s in parts)
                _sourceBuilder.Append(s);
        }

        _sourceBuilder.AppendLine();
    }

    public void Line(string line)
    {
        Line(parts: line);
    }

    public void BlockTab(Action writeInner)
    {
        BlockPrefix("\t", writeInner);
    }

    public void BlockPrefix(string delimiter, Action writeInner)
    {
        _indentPrefix.Append(delimiter);
        writeInner();
        _indentPrefix.Remove(_indentPrefix.Length - delimiter.Length, delimiter.Length);
    }

    public void BlockBrace(Action writeInner)
    {
        Line("{");
        BlockTab(writeInner);
        Line("}");
    }

    public void BlockDecl(Action writeInner, string suffix = ";")
    {
        Line("{");
        BlockTab(writeInner);
        Line($"}}{suffix}");
    }

    public void NamespaceBlockBrace(Action writeInner)
    {
        Line("namespace ", _typeSymbol.NamespaceOrEmpty());
        BlockBrace(writeInner);
    }

    public void NamespaceBlockBrace(string nameSpace, Action writeInner)
    {
        Line("namespace ", nameSpace);
        BlockBrace(writeInner);
    }

    public void AddCompilerGeneratedAttribute()
    {
        Line("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]");
    }

    public void AddGeneratedCodeAttribute()
    {
        Line(
            "[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"Shovel.SourceGenerators\", null)]"
        );
    }

    public void PartialTypeBlockBrace(Action writeInner)
    {
        var type = _typeSymbol.IsRecord
            ? "record"
            : _typeSymbol.IsValueType
                ? "struct"
                : "class";
        NamespaceBlockBrace(() =>
        {
            Line($"partial {type} {_typeSymbol.Name}");
            BlockBrace(writeInner);
        });
    }

    public void PartialTypeBlockBrace(string baseClassesOrImplementations, Action writeInner)
    {
        var type = _typeSymbol.IsRecord
            ? "record"
            : _typeSymbol.IsValueType
                ? "struct"
                : "class";

        NamespaceBlockBrace(() =>
        {
            Line($"partial {type} {_typeSymbol.Name} : {baseClassesOrImplementations}");
            BlockBrace(writeInner);
        });
    }

    public void Constructor(string[] args, Action writeInner)
    {
        var arguments = string.Join(", ", args);
        Line($"public {_typeSymbol.Name}({arguments})");
        BlockBrace(writeInner);
    }

    public void Constructor(string args, Action writeInner)
    {
        Line($"public {_typeSymbol.Name}({args})");
        BlockBrace(writeInner);
    }

    public void Constructor(Action writeInner)
    {
        Line($"public {_typeSymbol.Name}()");
        BlockBrace(writeInner);
    }

    public void StaticConstructor(Action writeInner)
    {
        Line($"static {_typeSymbol.Name}()");
        BlockBrace(writeInner);
    }

    public override string ToString()
    {
        _sourceBuilder.AppendLine();
        return _sourceBuilder.Append(BottomMessage).ToString();
    }
}
