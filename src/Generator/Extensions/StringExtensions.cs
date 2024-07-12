using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Generator.Extensions;

internal static class StringExtensions
{
    private const string SplitRegexStr = "[ _-]+|(?<=[a-z])(?=[A-Z])";

    private const string UnsafeCharsRegexStr = @"[^\w]+";

    private const string UnsafeFirstCharRegexStr = "^[^a-zA-Z_]+";

    private static readonly Regex SplitRegex =
        new(SplitRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    private static readonly Regex UnsafeCharsRegex =
        new(UnsafeCharsRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    private static readonly Regex UnsafeFirstCharRegex =
        new(UnsafeFirstCharRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    public static string ToTitleCase(this string source) =>
#pragma warning disable RS1035
        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(SplitRegex.Replace(source, " ").ToLower());
#pragma warning restore RS1035

    public static string ToSafeName(this string source)
    {
        source = source.ToTitleCase().Replace(" ", "");
        source = UnsafeCharsRegex.Replace(source, "_");
        return UnsafeFirstCharRegex.IsMatch(source) ? $"_{source}" : source;
    }

    public static string Truncate(this string source, int maxChars)
    {
        return source.Length <= maxChars ? source : source[..maxChars];
    }

    public static string SanitizeName(this string name, char replacementChar = '_')
    {
        var blackList = new HashSet<char>(Path.GetInvalidFileNameChars()) { '"' }; // '"' not invalid in Linux, but causes problems
        var output = name.ToCharArray();
        for (int i = 0, ln = output.Length; i < ln; i++)
        {
            if (blackList.Contains(output[i]))
            {
                output[i] = replacementChar;
            }
        }

        return new string(output);
    }

    public static string RemoveNameof(this string value)
    {
        value = value ?? throw new ArgumentNullException(nameof(value));

        return value.Contains("nameof(")
            ? value[(value.LastIndexOf('.') + 1)..].TrimEnd(')', ' ')
            : value;
    }
}
