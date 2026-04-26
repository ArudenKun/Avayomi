using System;

namespace Avayomi.Views;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ViewAttribute : Attribute
{
    public ViewAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
