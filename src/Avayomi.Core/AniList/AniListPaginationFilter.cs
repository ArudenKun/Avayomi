using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList;

public class AniListPaginationFilter
{
    public int PageIndex { get; }
    public int PageSize { get; }

    public AniListPaginationFilter(int pageIndex = 1, int pageSize = 20)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
    }

    internal IList<GqlParameter> ToParameters()
    {
        return [new GqlParameter("page", PageIndex), new GqlParameter("perPage", PageSize)];
    }
}
