namespace Core.Extensions;

public static class EnumerableExtensions
{
    public static int LastIndexOf<T>(this IEnumerable<T> source, T itemToFind)
    {
        return LastIndexOf(source, itemToFind, EqualityComparer<T>.Default);
    }

    public static int LastIndexOf<T>(
        this IEnumerable<T> source,
        T itemToFind,
        IEqualityComparer<T> equalityComparer
    )
    {
        ArgumentNullException.ThrowIfNull(source);

        var sourceArray = source.ToArray();

        for (var i = sourceArray.Length - 1; i >= 0; i--)
        {
            var currentItem = sourceArray[i];

            if (equalityComparer.Equals(currentItem, itemToFind))
            {
                return i;
            }
        }

        return -1;
    }

    public static bool IsEmpty<T>(this IEnumerable<T> source) => !source.Any();

    /// <summary>
    /// Splits the collection into two collections, containing the elements for which the given predicate returns True and False respectively. Element order is preserved in both of the created lists.
    /// </summary>
    public static (IEnumerable<T>, IEnumerable<T>) Partition<T>(
        this IEnumerable<T> me,
        Predicate<T> predicate
    )
    {
        var trueList = new List<T>();
        var falseList = new List<T>();

        foreach (var item in me)
        {
            if (predicate(item))
            {
                trueList.Add(item);
            }
            else
            {
                falseList.Add(item);
            }
        }

        return (trueList, falseList);
    }
}
