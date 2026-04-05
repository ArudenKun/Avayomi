using Avayomi.Core.AniList;
using Avayomi.Core.Animes;
using Avayomi.Core.GraphQL;
using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Volo.Abp.DependencyInjection;

namespace Avayomi.Core.Trackers.AniList;

internal sealed class AniListTracker : ITracker, ISingletonDependency
{
    private const string Url = "https://graphql.anilist.co";

    private readonly IGraphQLClient _graphQlClient;

    public AniListTracker(HttpClient httpClient)
    {
        _graphQlClient = new GraphQLHttpClient(Url, new SystemTextJsonSerializer());
    }

    public async Task<IReadOnlyList<TrackerInfoResult>> SearchAsync(
        string query,
        int page = 1,
        int perPage = 20,
        CancellationToken cancellationToken = default
    )
    {
        var results = new List<TrackerInfoResult>();
        var nextPage = true;
        for (var i = 0; i < page; i++)
        {
            if (!nextPage)
                break;

            var aniListPagination = await InternalSearchAsync(
                query,
                page + 1,
                perPage,
                cancellationToken
            );
            results.AddRange(aniListPagination.Data);
            nextPage = aniListPagination.PageInfo.HasNextPage;
        }

        return results;
    }

    private async Task<AniListPagination<TrackerInfoResult>> InternalSearchAsync(
        string query,
        int page = 1,
        int perPage = 20,
        CancellationToken cancellationToken = default
    )
    {
        var selection = new GqlSelection("Page")
        {
            Parameters = [new GqlParameter("page", page), new GqlParameter("perPage", perPage)],
            Selections =
            [
                new GqlSelection("pageInfo", GqlParser.ParseToSelections<AniListPageInfo>()),
                new GqlSelection(
                    "media",
                    GqlParser.ParseToSelections<TrackerInfoResult>(),
                    [
                        new GqlParameter("search", query),
                        new GqlParameter("type", AniListMediaType.Anime),
                    ]
                ),
            ],
        };
        var request = new GraphQLRequest { Query = selection.ToJsonString() };
        var response = await _graphQlClient.SendQueryAsync<AniListPagination<TrackerInfoResult>>(
            request,
            cancellationToken
        );
        if (!response.Errors.IsNullOrEmpty())
        {
            return new AniListPagination<TrackerInfoResult>(new AniListPageInfo(), []);
        }

        return response.Data;
    }

    public Task<TrackerInfoResult?> GetAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<TrackerInfoResult>> GetUserListAsync(
        AnimeStatus status = AnimeStatus.All,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<TrackerInfoResult?> UpdateAsync(
        string id,
        AnimeStatus status = AnimeStatus.All,
        int? score = null,
        int? progress = null,
        int? episodesWatched = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
