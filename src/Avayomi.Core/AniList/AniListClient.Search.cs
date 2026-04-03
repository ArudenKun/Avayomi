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
        AniListPaginationFilter? paginationFilter = null
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
        var response = await PostRequestAsync(selections);
        return new AniListPagination<Media>(
            GqlParser.ParseFromJson<PageInfo>(response["Page"]!["pageInfo"]!)!,
            GqlParser.ParseFromJson<Media[]>(response["Page"]!["media"]!)!
        );
    }

    public async Task<AniListPagination<Character>> SearchCharacterAsync(
        SearchCharacterFilter filter,
        AniListPaginationFilter? paginationFilter = null
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
        var response = await PostRequestAsync(selections);
        return new AniListPagination<Character>(
            GqlParser.ParseFromJson<PageInfo>(response["Page"]!["pageInfo"]!)!,
            GqlParser.ParseFromJson<Character[]>(response["Page"]!["characters"]!)!
        );
    }

    public async Task<AniListPagination<Staff>> SearchStaffAsync(
        SearchStaffFilter filter,
        AniListPaginationFilter? paginationFilter = null
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
        var response = await PostRequestAsync(selections);
        return new AniListPagination<Staff>(
            GqlParser.ParseFromJson<PageInfo>(response["Page"]!["pageInfo"]!)!,
            GqlParser.ParseFromJson<Staff[]>(response["Page"]!["staff"]!)!
        );
    }

    public async Task<AniListPagination<Studio>> SearchStudioAsync(
        SearchStudioFilter filter,
        AniListPaginationFilter? paginationFilter = null
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
        var response = await PostRequestAsync(selections);
        return new AniListPagination<Studio>(
            GqlParser.ParseFromJson<PageInfo>(response["Page"]!["pageInfo"])!,
            GqlParser.ParseFromJson<Studio[]>(response["Page"]!["studios"])!
        );
    }

    public async Task<AniListPagination<User>> SearchUserAsync(
        SearchUserFilter filter,
        AniListPaginationFilter? paginationFilter = null
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
        var response = await PostRequestAsync(selections);
        return new AniListPagination<User>(
            GqlParser.ParseFromJson<PageInfo>(response["Page"]!["pageInfo"])!,
            GqlParser.ParseFromJson<User[]>(response["Page"]!["users"])!
        );
    }

    public Task<AniListPagination<Media>> SearchMediaAsync(
        string query,
        AniListPaginationFilter? paginationFilter = null
    ) => SearchMediaAsync(new SearchMediaFilter { Query = query }, paginationFilter);

    public Task<AniListPagination<Character>> SearchCharacterAsync(
        string query,
        AniListPaginationFilter? paginationFilter = null
    )
    {
        return SearchCharacterAsync(new SearchCharacterFilter { Query = query }, paginationFilter);
    }

    public Task<AniListPagination<Staff>> SearchStaffAsync(
        string query,
        AniListPaginationFilter? paginationFilter = null
    )
    {
        return SearchStaffAsync(new SearchStaffFilter { Query = query }, paginationFilter);
    }

    public Task<AniListPagination<Studio>> SearchStudioAsync(
        string query,
        AniListPaginationFilter? paginationFilter = null
    )
    {
        return SearchStudioAsync(new SearchStudioFilter { Query = query }, paginationFilter);
    }

    public Task<AniListPagination<User>> SearchUserAsync(
        string query,
        AniListPaginationFilter? paginationFilter = null
    )
    {
        return SearchUserAsync(new SearchUserFilter { Query = query }, paginationFilter);
    }
}
