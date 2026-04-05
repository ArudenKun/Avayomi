using AutoInterfaceAttributes;
using Avayomi.Core.AniList.Models.Character;
using Avayomi.Core.AniList.Models.Media;
using Avayomi.Core.AniList.Models.Other;
using Avayomi.Core.AniList.Models.Staff;
using Avayomi.Core.AniList.Models.Studio;
using Avayomi.Core.AniList.Models.User;
using Avayomi.Core.AniList.Parameters;
using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList;

[AutoInterface]
internal partial class AniListClient
{
    public async Task<AniListPagination<Media>> SearchMediaAsync(
        SearchMediaFilter filter,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        paginationFilter ??= new AniListPaginationFilter();
        var selections = new GqlSelection("Page")
        {
            Parameters = paginationFilter.ToParameters(),
            Selections =
            [
                new GqlSelection("pageInfo", GqlParser.ParseToSelections<PageInfo>()),
                new GqlSelection(
                    "media",
                    GqlParser.ParseToSelections<Media>(),
                    filter.ToParameters()
                ),
            ],
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return new AniListPagination<Media>(
            GqlParser.ParseFromJson<PageInfo>(response["Page"]!["pageInfo"]!)!,
            GqlParser.ParseFromJson<Media[]>(response["Page"]!["media"]!)!
        );
    }

    public Task<AniListPagination<Media>> SearchMediaAsync(
        Action<SearchMediaFilter> configureFilter,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        var filter = new SearchMediaFilter();
        configureFilter(filter);
        return SearchMediaAsync(filter, paginationFilter, cancellationToken);
    }

    public async Task<AniListPagination<Character>> SearchCharacterAsync(
        SearchCharacterFilter filter,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        paginationFilter ??= new AniListPaginationFilter();
        var selections = new GqlSelection("Page")
        {
            Parameters = paginationFilter.ToParameters(),
            Selections =
            [
                new GqlSelection("pageInfo", GqlParser.ParseToSelections<PageInfo>()),
                new GqlSelection(
                    "characters",
                    GqlParser.ParseToSelections<Character>(),
                    filter.ToParameters()
                ),
            ],
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return new AniListPagination<Character>(
            GqlParser.ParseFromJson<PageInfo>(response["Page"]!["pageInfo"]!)!,
            GqlParser.ParseFromJson<Character[]>(response["Page"]!["characters"]!)!
        );
    }

    public Task<AniListPagination<Character>> SearchCharacterAsync(
        Action<SearchCharacterFilter> configureFilter,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        var filter = new SearchCharacterFilter();
        configureFilter(filter);
        return SearchCharacterAsync(filter, paginationFilter, cancellationToken);
    }

    public async Task<AniListPagination<Staff>> SearchStaffAsync(
        SearchStaffFilter filter,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        paginationFilter ??= new AniListPaginationFilter();
        var selections = new GqlSelection("Page")
        {
            Parameters = paginationFilter.ToParameters(),
            Selections =
            [
                new GqlSelection("pageInfo", GqlParser.ParseToSelections<PageInfo>()),
                new GqlSelection(
                    "staff",
                    GqlParser.ParseToSelections<Staff>(),
                    filter.ToParameters()
                ),
            ],
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return new AniListPagination<Staff>(
            GqlParser.ParseFromJson<PageInfo>(response["Page"]!["pageInfo"]!)!,
            GqlParser.ParseFromJson<Staff[]>(response["Page"]!["staff"]!)!
        );
    }

    public Task<AniListPagination<Staff>> SearchStaffAsync(
        Action<SearchStaffFilter> configureFilter,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        var filter = new SearchStaffFilter();
        configureFilter(filter);
        return SearchStaffAsync(filter, paginationFilter, cancellationToken);
    }

    public async Task<AniListPagination<Studio>> SearchStudioAsync(
        SearchStudioFilter filter,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        paginationFilter ??= new AniListPaginationFilter();
        var selections = new GqlSelection("Page")
        {
            Parameters = paginationFilter.ToParameters(),
            Selections =
            [
                new GqlSelection("pageInfo", GqlParser.ParseToSelections<PageInfo>()),
                new GqlSelection(
                    "studios",
                    GqlParser.ParseToSelections<Studio>(),
                    filter.ToParameters()
                ),
            ],
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return new AniListPagination<Studio>(
            GqlParser.ParseFromJson<PageInfo>(response["Page"]!["pageInfo"]!)!,
            GqlParser.ParseFromJson<Studio[]>(response["Page"]!["studios"]!)!
        );
    }

    public Task<AniListPagination<Studio>> SearchStudioAsync(
        Action<SearchStudioFilter> configureFilter,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        var filter = new SearchStudioFilter();
        configureFilter(filter);
        return SearchStudioAsync(filter, paginationFilter, cancellationToken);
    }

    public async Task<AniListPagination<User>> SearchUserAsync(
        SearchUserFilter filter,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        paginationFilter ??= new AniListPaginationFilter();
        var selections = new GqlSelection("Page")
        {
            Parameters = paginationFilter.ToParameters(),
            Selections =
            [
                new GqlSelection("pageInfo", GqlParser.ParseToSelections<PageInfo>()),
                new GqlSelection(
                    "users",
                    GqlParser.ParseToSelections<User>(),
                    filter.ToParameters()
                ),
            ],
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return new AniListPagination<User>(
            GqlParser.ParseFromJson<PageInfo>(response["Page"]!["pageInfo"]!)!,
            GqlParser.ParseFromJson<User[]>(response["Page"]!["users"]!)!
        );
    }

    public Task<AniListPagination<User>> SearchUserAsync(
        Action<SearchUserFilter> configureFilter,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        var filter = new SearchUserFilter();
        configureFilter(filter);
        return SearchUserAsync(filter, paginationFilter, cancellationToken);
    }

    public Task<AniListPagination<Media>> SearchMediaAsync(
        string query,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    ) =>
        SearchMediaAsync(
            new SearchMediaFilter { Query = query },
            paginationFilter,
            cancellationToken
        );

    public Task<AniListPagination<Character>> SearchCharacterAsync(
        string query,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    ) =>
        SearchCharacterAsync(
            new SearchCharacterFilter { Query = query },
            paginationFilter,
            cancellationToken
        );

    public Task<AniListPagination<Staff>> SearchStaffAsync(
        string query,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    ) =>
        SearchStaffAsync(
            new SearchStaffFilter { Query = query },
            paginationFilter,
            cancellationToken
        );

    public Task<AniListPagination<Studio>> SearchStudioAsync(
        string query,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    ) =>
        SearchStudioAsync(
            new SearchStudioFilter { Query = query },
            paginationFilter,
            cancellationToken
        );

    public Task<AniListPagination<User>> SearchUserAsync(
        string query,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    ) =>
        SearchUserAsync(
            new SearchUserFilter { Query = query },
            paginationFilter,
            cancellationToken
        );
}
