using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CodeGenHelpers;
using Microsoft.CodeAnalysis;

namespace Avayomi.SourceGenerator.AvaloniaResource;

internal static class CodeGenerator
{
    public static string GenerateResource(string @namespace, string modifier)
    {
        var accessModifier = modifier switch
        {
            "public" => Accessibility.Public,
            _ => Accessibility.Internal,
        };

        var codeBuilder = CodeBuilder.Create(@namespace);
        var classBuilder = codeBuilder
            .AddClass("AvaloniaResource")
            .AddNamespaceImport("System")
            .AddNamespaceImport("System.Collections.Concurrent")
            .AddNamespaceImport("System.IO")
            .AddNamespaceImport("System.Linq")
            .AddNamespaceImport("System.Net")
            .AddNamespaceImport("System.Reflection")
            .AddNamespaceImport("Avalonia.Controls")
            .AddNamespaceImport("Avalonia.Media.Imaging")
            .AddNamespaceImport("Avalonia.Platform")
            .WithAccessModifier(accessModifier);

        classBuilder
            .AddProperty("AssetCache", Accessibility.Private)
            .SetType("ConcurrentDictionary<string, Uri>")
            .MakeStatic()
            .WithReadonlyValue("new ConcurrentDictionary<string, Uri>()");

        classBuilder
            .AddConstructor(Accessibility.Public)
            .AddParameter("string", "fileName")
            .WithBody(writer =>
            {
                writer.AppendLine("FileName = fileName;");
            });

        classBuilder
            .AddProperty("FileName", Accessibility.Public)
            .SetType<string>()
            .UseGetOnlyAutoProp();

        classBuilder
            .AddMethod("AsStream", Accessibility.Public)
            .WithReturnType("Stream")
            .WithSummary(
                """
                Searches for a file among Embedded resources <br/>
                Throws an <see cref="ArgumentException"/> if nothing is found or more than one match is found <br/>
                """
            )
            .WithParameterDoc(
                "assembly",
                "The assembly to look for embedded resources (Defaults: Executing assembly"
            )
            .AddParameterWithNullValue("Assembly?", "assembly")
            .WithBody(writer =>
            {
                writer.AppendLine("assembly ??= Assembly.GetExecutingAssembly();");
                using (writer.Block("try"))
                {
                    writer.AppendLine(
                        "var assetUri = AssetCache.GetOrAdd(FileName, key => AssetLoader"
                    );
                    writer.AppendLine(
                        ".GetAssets(new Uri($\"avares://{assembly.GetName().Name}\"), null)"
                    );
                    writer.AppendLine(
                        ".Single(s => WebUtility.UrlDecode(s.AbsoluteUri).EndsWith($\"{key}\", StringComparison.InvariantCultureIgnoreCase))) "
                    );
                    writer.AppendLine(
                        "?? throw new ArgumentException($\"\\\"{FileName}\\\" is not found in embedded resources\");"
                    );
                    writer.AppendLine("return AssetLoader.Open(assetUri);");
                }

                using (writer.Block("catch (InvalidOperationException exception)"))
                {
                    writer.AppendLine(
                        """
                        throw new ArgumentException(
                                       "Not a single one was found or more than one resource with the given name was found. "
                                           + "Make sure there are no collisions and the required file has the attribute \"AvaloniaResource\"",
                                       exception);
                        """
                    );
                }
            });

        classBuilder
            .AddMethod("AsBytes", Accessibility.Public)
            .WithReturnType("byte[]")
            .WithParameterDoc(
                "assembly",
                "The assembly to look for embedded resources (Defaults: Executing assembly"
            )
            .AddParameterWithNullValue("Assembly?", "assembly")
            .WithBody(writer =>
            {
                writer.AppendLine("using var stream = AsStream(assembly);");
                writer.AppendLine("using var memoryStream = new MemoryStream();");
                writer.AppendLine("stream.CopyTo(memoryStream);");
                writer.AppendLine("return memoryStream.ToArray();");
            });

        classBuilder
            .AddMethod("AsString", Accessibility.Public)
            .WithReturnType("string")
            .WithParameterDoc(
                "assembly",
                "The assembly to look for embedded resources (Defaults: Executing assembly"
            )
            .AddParameterWithNullValue("Assembly?", "assembly")
            .WithBody(writer =>
            {
                writer.AppendLine("using var stream = AsStream(assembly);");
                writer.AppendLine("using var reader = new StreamReader(stream);");
                writer.AppendLine("return reader.ReadToEnd();");
            });

        classBuilder
            .AddMethod("AsBitmap", Accessibility.Public)
            .WithReturnType("Bitmap")
            .WithParameterDoc(
                "assembly",
                "The assembly to look for embedded resources (Defaults: Executing assembly"
            )
            .AddParameterWithNullValue("Assembly?", "assembly")
            .WithBody(writer =>
            {
                writer.AppendLine("using var stream = AsStream(assembly);");
                writer.AppendLine("return new Bitmap(stream);");
            });

        classBuilder
            .AddMethod("AsWindowIcon", Accessibility.Public)
            .WithReturnType("WindowIcon")
            .WithParameterDoc(
                "assembly",
                "The assembly to look for embedded resources (Defaults: Executing assembly"
            )
            .AddParameterWithNullValue("Assembly?", "assembly")
            .WithBody(writer =>
            {
                writer.AppendLine("var bitmap = AsBitmap(assembly);");
                writer.AppendLine("return new WindowIcon(bitmap);");
            });

        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine(codeBuilder.Build());

        return sb.ToString();
    }

    public static string GenerateResources(
        string @namespace,
        string modifier,
        string className,
        IReadOnlyCollection<Resource> resources
    )
    {
        var accessModifier = modifier switch
        {
            "public" => Accessibility.Public,
            _ => Accessibility.Internal,
        };

        var properties = resources
            .Select(static resource =>
                (
                    Name: SanitizeName(Path.GetFileName(resource.Path)),
                    FileName: Path.GetFileName(resource.Path)
                )
            )
            .ToArray();

        var codeBuilder = CodeBuilder.Create(@namespace);
        var classBuilder = codeBuilder
            .AddClass(className)
            .MakeStaticClass()
            .WithAccessModifier(accessModifier);

        foreach (var property in properties)
        {
            classBuilder
                .AddProperty(property.Name, Accessibility.Public)
                .SetType("AvaloniaResource")
                .MakeStatic()
                .WithGetterExpression($"new AvaloniaResource(\"{property.FileName}\")");
        }

        return codeBuilder.Build();
    }

    internal static string SanitizeName(string? name)
    {
        static bool InvalidFirstChar(char ch) =>
            ch is not ('_' or >= 'A' and <= 'Z' or >= 'a' and <= 'z');

        static bool InvalidSubsequentChar(char ch) =>
            ch is not ('_' or >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9');

        if (string.IsNullOrEmpty(name))
        {
            return "";
        }

        if (InvalidFirstChar(name![0]))
        {
            name = "_" + name;
        }

        if (!name.Skip(1).Any(InvalidSubsequentChar))
        {
            return name;
        }

        Span<char> buf = stackalloc char[name.Length];
        name.AsSpan().CopyTo(buf);

        for (var i = 1; i < buf.Length; i++)
        {
            if (InvalidSubsequentChar(buf[i]))
            {
                buf[i] = '_';
            }
        }

        // Span<char>.ToString implementation checks for char type, new string(&buf[0], buf.length)
        return buf.ToString();
    }
}
