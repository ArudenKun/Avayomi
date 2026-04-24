using System;
using System.Collections.Generic;
using System.Text;

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
