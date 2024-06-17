using System.Collections.Generic;
using Avayomi.Generators.Abstractions;

namespace Avayomi.Generators.Statics;

internal class AttributesStatic : StaticGenerator
{
    private const string SourceText = $"""
        namespace {MetadataNames.Namespace}.Attributes;

        using System;

        [AttributeUsage(AttributeTargets.Class)]
        public sealed class IgnoreAttribute : Attribute;

        [AttributeUsage(AttributeTargets.Class)]
        public sealed class SingletonAttribute : Attribute;

        [AttributeUsage(AttributeTargets.Class)]
        public class StaticViewLocatorAttribute : Attribute;
        """;

    public override IEnumerable<(string FileName, string Source)> Generate()
    {
        yield return ($"{MetadataNames.Namespace}.Attributes.g.cs", SourceText);
    }
}
