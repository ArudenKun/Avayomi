using System;
using System.Linq;
using System.Text;

namespace Avayomi.Generators.Utilities;

internal static class StringExtensions
{
    private static readonly char[] Delimeters = [' ', '-', '_'];

    public static string Truncate(this string source, int maxChars) =>
        source.Length <= maxChars ? source : source[..maxChars];

    public static string ToCamelCase(this string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return SymbolsPipe(
            source,
            '\0',
            (s, disableFrontDelimeter) =>
            {
                if (disableFrontDelimeter)
                {
                    return [char.ToLowerInvariant(s)];
                }

                return [char.ToUpperInvariant(s)];
            }
        );
    }

    public static string ToKebabCase(this string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return SymbolsPipe(
            source,
            '-',
            (s, disableFrontDelimeter) =>
            {
                if (disableFrontDelimeter)
                {
                    return [char.ToLowerInvariant(s)];
                }

                return ['-', char.ToLowerInvariant(s)];
            }
        );
    }

    public static string ToPascalCase(this string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return SymbolsPipe(source, '\0', (s, _) => [char.ToUpperInvariant(s)]);
    }

    public static string ToSnakeCase(this string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return SymbolsPipe(
            source,
            '_',
            (s, disableFrontDelimeter) =>
            {
                if (disableFrontDelimeter)
                {
                    return [char.ToLowerInvariant(s)];
                }

                return ['_', char.ToLowerInvariant(s)];
            }
        );
    }

    public static string ToTrainCase(this string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return SymbolsPipe(
            source,
            '-',
            (s, disableFrontDelimeter) =>
            {
                if (disableFrontDelimeter)
                {
                    return [char.ToUpperInvariant(s)];
                }

                return ['-', char.ToUpperInvariant(s)];
            }
        );
    }

    public static string ToPrivateCase(this string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return "_" + char.ToLower(source[0]) + source[1..];
    }

    private static string SymbolsPipe(
        string source,
        char mainDelimeter,
        Func<char, bool, char[]> newWordSymbolHandler
    )
    {
        var builder = new StringBuilder();

        bool nextSymbolStartsNewWord = true;
        bool disableFrontDelimeter = true;
        for (var i = 0; i < source.Length; i++)
        {
            var symbol = source[i];
            if (Delimeters.Contains(symbol))
            {
                if (symbol == mainDelimeter)
                {
                    builder.Append(symbol);
                    disableFrontDelimeter = true;
                }

                nextSymbolStartsNewWord = true;
            }
            else if (!char.IsLetter(symbol))
            {
                builder.Append(symbol);
                disableFrontDelimeter = true;
                nextSymbolStartsNewWord = true;
            }
            else
            {
                if (nextSymbolStartsNewWord || char.IsUpper(symbol))
                {
                    builder.Append(newWordSymbolHandler(symbol, disableFrontDelimeter));
                    disableFrontDelimeter = false;
                    nextSymbolStartsNewWord = false;
                }
                else
                {
                    builder.Append(symbol);
                }
            }
        }

        return builder.ToString();
    }
}
