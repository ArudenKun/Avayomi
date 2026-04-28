using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avayomi.Core;
using PleasantUI.Controls;

namespace Avayomi.Converters;

public class ProgressToContentConverter : SingletonBase<ProgressToContentConverter>, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
            return new ProgressRing
            {
                Width = 96,
                Height = 96,
                Thickness = 6,
            };

        return new Panel();
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        throw new NotSupportedException();
    }
}
