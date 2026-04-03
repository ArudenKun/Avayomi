using Avayomi.Core.AniList.Models.Other;

namespace Avayomi.Core.AniList;

public sealed class AniListPagination<TData>
{
    public int TotalCount { get; }
    public int PerPageCount { get; }
    public int CurrentPageIndex { get; }
    public int LastPageIndex { get; }
    public bool HasNextPage { get; }
    public TData[] Data { get; }

    internal AniListPagination(PageInfo pageInfo, TData[] data)
    {
        TotalCount = pageInfo.TotalCount;
        PerPageCount = pageInfo.PerPageCount;
        CurrentPageIndex = pageInfo.CurrentPageIndex;
        LastPageIndex = pageInfo.LastPageIndex;
        HasNextPage = pageInfo.HasNextPage;
        Data = data;
    }
}
