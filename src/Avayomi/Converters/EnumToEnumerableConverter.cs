using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avayomi.Core;
using Avayomi.Core.Extensions;

namespace Avayomi.Converters;

public class EnumToEnumerableConverter : SingletonBase<EnumToEnumerableConverter>, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Enum enumValue)
        {
            return enumValue.GetAllValues();
        }

        return null;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        var parameterString = parameter?.ToString();
        if (string.IsNullOrWhiteSpace(parameterString))
            return null;
        return Enum.TryParse(targetType, parameterString, true, out var result) ? result : null;
    }
}
