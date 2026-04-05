namespace Avayomi.Core.Trackers.AniList;

internal sealed class AniListPagination<TData>
{
    public AniListPageInfo PageInfo { get; }
    public TData[] Data { get; }

    internal AniListPagination(AniListPageInfo pageInfo, TData[] data)
    {
        PageInfo = pageInfo;
        Data = data;
    }
}
