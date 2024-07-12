﻿using System;
using Generator.Metadata;
using Generator.Metadata.CopyCode;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Generator.Attributes;

#if GENERATOR
[GenerateFactory]
#endif
[Copy]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public partial class AvaloniaPropertyAttribute : Attribute
{
    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="type"></param>
    /// <exception cref="global::System.ArgumentNullException"></exception>
    public AvaloniaPropertyAttribute(string name, Type type)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

#if GENERATOR
    [ExcludeFromFactory]
    public AvaloniaPropertyAttribute(string name, object? typeSymbolContainer)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _typeSymbolContainer = typeSymbolContainer;
    }
#endif

    /// <summary>
    ///     Name of this avalonia property.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Type of this avalonia property.
    /// </summary>
    public Type Type { get; }

    public Type[] Attributes { get; set; } = [];
}

[Copy]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AvaloniaPropertyAttribute<T> : AvaloniaPropertyAttribute
{
    public AvaloniaPropertyAttribute(string name)
        : base(name, typeof(T)) { }
}
