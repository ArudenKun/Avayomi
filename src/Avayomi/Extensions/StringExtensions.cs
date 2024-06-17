using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Avayomi.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Formats a string using invariant culture. This is a shortcut for string.format(CultureInfo.InvariantCulture, ...)
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <returns>The formatted string.</returns>
    public static string FormatInvariant(this string format, params object?[] args) =>
        string.Format(CultureInfo.InvariantCulture, format, args);

    /// <summary>
    /// Removes one leading occurrence of the specified string
    /// </summary>
    public static string TrimStart(
        this string me,
        string trimString,
        StringComparison comparisonType
    ) => me.StartsWith(trimString, comparisonType) ? me[trimString.Length..] : me;

    /// <summary>
    /// Removes one trailing occurrence of the specified string
    /// </summary>
    public static string TrimEnd(
        this string me,
        string trimString,
        StringComparison comparisonType
    ) => me.EndsWith(trimString, comparisonType) ? me[..^trimString.Length] : me;

    /// <summary>
    /// Returns true if the string contains leading or trailing whitespace, otherwise returns false.
    /// </summary>
    public static bool IsTrimmable(this string me)
    {
        if (me.Length == 0)
        {
            return false;
        }

        return char.IsWhiteSpace(me[0]) || char.IsWhiteSpace(me[^1]);
    }

    public static string[] SplitWords(this string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    public static string[] SplitLines(this string text, int lineWidth)
    {
        static void InternalSplit(string text, int lineWidth, List<string> result)
        {
            while (true)
            {
                if (text.Length < lineWidth)
                {
                    result.Add(text);
                    return;
                }

                var line = text.SplitWords()
                    .ScanElements(string.Empty, (l, w) => l + w + ' ')
                    .TakeWhile(l => l.Length <= lineWidth)
                    .DefaultIfEmpty(text)
                    .Last();
                result.Add(line);
                text = text[(line.Length)..];
            }
        }

        List<string> result = [];
        InternalSplit(text, lineWidth, result);
        return result.ToArray();
    }
}
