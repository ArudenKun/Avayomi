namespace Generator.Extensions;

internal static class StringExtensions
{
    public static string Truncate(this string source, int maxChars) =>
        source.Length <= maxChars ? source : source[..maxChars];
}
