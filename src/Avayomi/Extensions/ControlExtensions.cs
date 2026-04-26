using System;
using System.Reflection;
using Avalonia;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avayomi.Views;

namespace Avayomi.Extensions;

public static class ControlExtensions
{
    extension(Type type)
    {
        public string ViewName => type.GetViewName();
    }

    public static string GetViewName(this Type viewType)
    {
        var viewName = viewType.Name;
        var viewAttribute = viewType.GetSingleAttributeOrNull<ViewAttribute>(false);
        if (viewAttribute is not null)
        {
            viewName = viewAttribute.Name;
        }

        return viewName;
    }

    public static T DynamicResource<T>(this T control, AvaloniaProperty prop, object resourceKey)
        where T : Control
    {
        control[!prop] = new DynamicResourceExtension(resourceKey);
        return control;
    }

    public static Style DynamicResource(this Style style, AvaloniaProperty prop, object resourceKey)
    {
        style.Setters.Add(
            new Setter() { Property = prop, Value = new DynamicResourceExtension(resourceKey) }
        );
        return style;
    }
}
