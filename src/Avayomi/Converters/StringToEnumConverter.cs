using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using ZLinq;

namespace Avayomi.Converters;

public class StringToEnumConverter : SingletonBase<StringToEnumConverter>, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return null;
        var list = value.GetType().GetAllValues().AsValueEnumerable();
        return list.FirstOrDefault(vd => Equals(vd, value));
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is null)
            return null;
        var list = value.GetType().GetAllValues().AsValueEnumerable();
        return list.FirstOrDefault(vd => Equals(vd, value));
    }
}
