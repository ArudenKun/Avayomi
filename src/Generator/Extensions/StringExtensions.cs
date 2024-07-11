using System.Globalization;
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
}
