using System;
using Avayomi.Resources;

namespace Avayomi.Extensions;

public static class ValidatorExtensions
{
    /// <summary>
    /// Returns whether specified value is in valid range.
    /// </summary>
    /// <typeparam name="T">The type of data to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum valid value.</param>
    /// <param name="minInclusive">Whether the minimum value is valid.</param>
    /// <param name="max">The maximum valid value.</param>
    /// <param name="maxInclusive">Whether the maximum value is valid.</param>
    /// <returns>Whether the value is within range.</returns>
    public static bool IsInRange<T>(
        this T value,
        T? min = null,
        bool minInclusive = true,
        T? max = null,
        bool maxInclusive = true
    )
        where T : struct, IComparable<T>
    {
        var minValid =
            min == null
            || (minInclusive && value.CompareTo(min.Value) >= 0)
            || (!minInclusive && value.CompareTo(min.Value) > 0);
        var maxValid =
            max == null
            || (maxInclusive && value.CompareTo(max.Value) <= 0)
            || (!maxInclusive && value.CompareTo(max.Value) < 0);
        return minValid && maxValid;
    }

    /// <summary>
    /// Validates whether specified value is in valid range, and throws an exception if out of range.
    /// </summary>
    /// <typeparam name="T">The type of data to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="min">The minimum valid value.</param>
    /// <param name="minInclusive">Whether the minimum value is valid.</param>
    /// <param name="max">The maximum valid value.</param>
    /// <param name="maxInclusive">Whether the maximum value is valid.</param>
    /// <returns>The value if valid.</returns>
    public static T CheckRange<T>(
        this T value,
        string name,
        T? min = null,
        bool minInclusive = true,
        T? max = null,
        bool maxInclusive = true
    )
        where T : struct, IComparable<T>
    {
        if (!value.IsInRange(min, minInclusive, max, maxInclusive))
        {
            if (min.HasValue && minInclusive && max.HasValue && maxInclusive)
            {
                var message = Local.ValueRangeBetween;
                throw new ArgumentOutOfRangeException(
                    name,
                    value,
                    message.FormatInvariant(name, min, max)
                );
            }
            else
            {
                var message = value.GetRangeError(name, min, minInclusive, max, maxInclusive);
                throw new ArgumentOutOfRangeException(name, value, message);
            }
        }

        return value;
    }

    /// <summary>
    /// Returns the range validation message.
    /// </summary>
    /// <typeparam name="T">The type of data to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="min">The minimum valid value.</param>
    /// <param name="minInclusive">Whether the minimum value is valid.</param>
    /// <param name="max">The maximum valid value.</param>
    /// <param name="maxInclusive">Whether the maximum value is valid.</param>
    /// <returns>The range validation message.</returns>
    public static string? GetRangeError<T>(
        this T value,
        string name,
        T? min = null,
        bool minInclusive = true,
        T? max = null,
        bool maxInclusive = true
    )
        where T : struct, IComparable<T>
    {
        if (value.IsInRange(min, minInclusive, max, maxInclusive))
        {
            return null;
        }

        var messageMin = min.HasValue ? GetOpText(true, minInclusive).FormatInvariant(min) : null;
        var messageMax = max.HasValue ? GetOpText(false, maxInclusive).FormatInvariant(max) : null;
        var message =
            messageMin != null && messageMax != null
                ? Local.ValueRangeAnd
                : Local.ValueRange;
        return message.FormatInvariant(name, messageMin ?? messageMax, messageMax);
    }

    private static string GetOpText(bool greaterThan, bool inclusive) =>
        greaterThan && inclusive
            ? Local.ValueRangeGreaterThanInclusive
            : greaterThan
                ? Local.ValueRangeGreaterThan
                : inclusive
                    ? Local.ValueRangeLessThanInclusive
                    : Local.ValueRangeLessThan;
}