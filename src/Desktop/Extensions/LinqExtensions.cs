using System;
using System.Collections.Generic;
using System.Linq;

namespace Desktop.Extensions;

public static class LinqExtensions
{
    public static T? RandomElement<T>(this IEnumerable<T> source)
    {
        T? current = default;
        var count = 0;
        foreach (var element in source)
        {
            count++;

            if (Random.Shared.Next(0, count) == 0)
                current = element;
        }

        return current;
    }

    /// <summary>
    ///     Selects a random element based on order bias.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="biasPercent">1-100, eg. if 80, then 80% probability for the first element.</param>
    public static T? BiasedRandomElement<T>(this IEnumerable<T> source, int biasPercent)
    {
        foreach (var element in source)
            if (Random.Shared.Next(1, 101) <= biasPercent)
                return element;

        return source.Any() ? source.First() : default;
    }

    public static IList<T> Shuffle<T>(this IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = Random.Shared.Next(0, n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }

        return list;
    }

    public static IEnumerable<TAccumulate> ScanElements<TSource, TAccumulate>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> func
    )
    {
        var previous = seed;
        foreach (var item in source)
        {
            previous = func(previous, item);
            yield return previous;
        }
    }
}
