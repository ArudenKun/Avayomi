using System.Runtime.CompilerServices;
using Ardalis.GuardClauses;

namespace Core.Extensions;

public static class GuardExtensions
{
    /// <summary>
    /// Returns whether specified value is in valid range.
    /// </summary>
    /// <typeparam name="T">The type of data to validate.</typeparam>
    /// <param name="guardClause"></param>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum valid value.</param>
    /// <param name="minInclusive">Whether the minimum value is valid.</param>
    /// <param name="max">The maximum valid value.</param>
    /// <param name="maxInclusive">Whether the maximum value is valid.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>Whether the value is within range.</returns>
    public static bool IsInRange<T>(
        this IGuardClause guardClause,
        T value,
        T? min = null,
        bool minInclusive = true,
        T? max = null,
        bool maxInclusive = true,
        [CallerArgumentExpression("value")] string? parameterName = null
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
    /// <param name="guardClause"></param>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum valid value.</param>
    /// <param name="max">The maximum valid value.</param>
    /// <param name="minInclusive">Whether the minimum value is valid.</param>
    /// <param name="maxInclusive">Whether the maximum value is valid.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The value if valid.</returns>
    public static T CheckRange<T>(
        this IGuardClause guardClause,
        T value,
        T? min = null,
        T? max = null,
        bool minInclusive = true,
        bool maxInclusive = true,
        [CallerArgumentExpression("value")] string? parameterName = null
    )
        where T : struct, IComparable<T>
    {
        if (!guardClause.IsInRange(value, min, minInclusive, max, maxInclusive))
        {
            if (min.HasValue && minInclusive && max.HasValue && maxInclusive)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    $"{parameterName} must be between {min} and {max}"
                );
            }

            var message = guardClause.GetRangeError(
                value,
                min,
                max,
                minInclusive,
                maxInclusive,
                parameterName
            );
            throw new ArgumentOutOfRangeException(parameterName, value, message);
        }

        return value;
    }

    /// <summary>
    /// Returns the range validation message.
    /// </summary>
    /// <typeparam name="T">The type of data to validate.</typeparam>
    /// <param name="guardClause"></param>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum valid value.</param>
    /// <param name="max">The maximum valid value.</param>
    /// <param name="minInclusive">Whether the minimum value is valid.</param>
    /// <param name="maxInclusive">Whether the maximum value is valid.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The range validation message.</returns>
    public static string? GetRangeError<T>(
        this IGuardClause guardClause,
        T value,
        T? min = null,
        T? max = null,
        bool minInclusive = true,
        bool maxInclusive = true,
        [CallerArgumentExpression("value")] string? parameterName = null
    )
        where T : struct, IComparable<T>
    {
        if (guardClause.IsInRange(value, min, minInclusive, max, maxInclusive))
        {
            return null;
        }

        var messageMin = min.HasValue ? GetOpText(true, minInclusive).FormatInvariant(min) : null;
        var messageMax = max.HasValue ? GetOpText(false, maxInclusive).FormatInvariant(max) : null;
        var message =
            messageMin != null && messageMax != null
                ? "{0} must be {1} and {2}."
                : "{0} must be {1}.";
        return message.FormatInvariant(parameterName ?? "", messageMin ?? messageMax, messageMax);
    }

    private static string GetOpText(bool greaterThan, bool inclusive) =>
        greaterThan && inclusive
            ? "greater than or equal to {0}"
            : greaterThan
                ? "greater than {0}"
                : inclusive
                    ? "less than or equal to {0}"
                    : "less than {0}";
}
