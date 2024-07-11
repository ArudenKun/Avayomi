using System;
using System.Collections.Generic;
using Ardalis.GuardClauses;

namespace Desktop.Extensions;

public static class ListExtensions
{
    public static void RemoveFirst<T>(this List<T> source)
    {
        source.RemoveAt(0);
    }

    public static void RemoveLast<T>(this List<T> source)
    {
        source.RemoveAt(source.Count - 1);
    }

    public static int BinarySearchIndexOf<T, TKey>(this IList<T> list, TKey value)
        where T : IComparable<TKey>
    {
        Guard.Against.Null(list);

        var lower = 0;
        var upper = list.Count - 1;

        while (lower <= upper)
        {
            var middle = lower + (upper - lower) / 2;
            var comparisonResult = list[middle].CompareTo(value);

            if (comparisonResult < 0)
                lower = middle + 1;
            else if (comparisonResult > 0)
                upper = middle - 1;
            else
                return middle;
        }

        return ~lower;
    }

    /// <summary>
    ///     Inserts an element into the collection, keeping it sorted. The collection must be sorted
    ///     already, i.e. populated only with this method. The template type for the collection must
    ///     implement IComparable.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The "this" reference.</param>
    /// <param name="item">The item to insert.</param>
    /// <param name="exclusive">True if duplicate items are allowed, False if they are not allowed.</param>
    /// <returns>True if an item was inserted, and false if no item was inserted.</returns>
    public static bool InsertSorted<T>(this IList<T> source, T item, bool exclusive = true)
        where T : IComparable<T>
    {
        var index = source.BinarySearchIndexOf(item);

        if (index < 0)
        {
            source.Insert(~index, item);
            return true;
        }

        if (!exclusive)
        {
            source.Insert(index, item);
            return true;
        }

        return false;
    }
}
