using System;
using System.Reflection;
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
}
