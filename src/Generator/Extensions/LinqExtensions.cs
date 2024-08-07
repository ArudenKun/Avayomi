﻿using System.Collections.Generic;

namespace Generator.Extensions;

internal static class LinqExtensions
{
    public static TValue? Get<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key)
    {
        return source.TryGetValue(key, out var value) ? value : default;
    }

    public static string AsCommaSeparated<T>(this IEnumerable<T> items, string? suffixes = null)
    {
        return string.Join($",{suffixes}", items);
    }
}
