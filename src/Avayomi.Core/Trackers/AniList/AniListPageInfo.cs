using Avayomi.Core.GraphQL;

namespace Avayomi.Core.Trackers.AniList;

internal sealed class AniListPageInfo
{
    /// <summary>
    /// The total number of items. Note: This value is not guaranteed to be accurate, do not rely on this for logic.
    /// </summary>
    [GqlSelection("total")]
    public int TotalCount { get; init; }

    /// <summary>
    /// The count on a page.
    /// </summary>
    [GqlSelection("perPage")]
    public int PerPageCount { get; init; }

    /// <summary>
    /// The current page.
    /// </summary>
    [GqlSelection("currentPage")]
    public int CurrentPageIndex { get; init; }

    /// <summary>
    /// The last page.
    /// </summary>
    [GqlSelection("lastPage")]
    public int LastPageIndex { get; init; }

    /// <summary>
    /// If there is another page.
    /// </summary>
    [GqlSelection("hasNextPage")]
    public bool HasNextPage { get; init; }
}
