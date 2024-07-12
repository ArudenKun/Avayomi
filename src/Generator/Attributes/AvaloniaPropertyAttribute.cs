using System;
using Avalonia.Data;

namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AvaloniaPropertyAttribute : Attribute
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

    /// <summary>
    ///     Name of this avalonia property.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Type of this avalonia property.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Default value of this dependency property. <br/>
    /// If you need to pass a new() expression, use <see cref="DefaultValueExpression"/>. <br/>
    /// Default - <see langword="default(type)"/>.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Default value expression of this dependency property. <br/>
    /// Used to pass a new() expression to an initializer. <br/>
    /// Default - <see langword="null"/>.
    /// </summary>
    public string? DefaultValueExpression { get; set; }

    /// <summary>
    /// The property will create through RegisterReadOnly (if the platform supports it) and
    /// the property setter will contain the protected modifier. <br/>
    /// Default - <see langword="false"/>.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Avalonia: Direct properties are a lightweight version of styled properties. <br/>
    /// Default - <see langword="false"/>.
    /// </summary>
    public bool IsDirect { get; set; }

    /// <summary>
    /// Default BindingMode. <br/>
    /// </summary>
    public BindingMode DefaultBindingMode { get; set; } = BindingMode.Default;

    /// <summary>
    /// The attributes to pass to the generated property
    /// </summary>
    public Type[] PropertyAttributes { get; set; } = [];
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AvaloniaPropertyAttribute<T> : AvaloniaPropertyAttribute
{
    public AvaloniaPropertyAttribute(string name)
        : base(name, typeof(T)) { }
}
