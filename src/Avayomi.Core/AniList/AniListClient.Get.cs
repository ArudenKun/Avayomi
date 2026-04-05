using AutoInterfaceAttributes;
using Avayomi.Core.AniList.Models.Character;
using Avayomi.Core.AniList.Models.Media;
using Avayomi.Core.AniList.Models.Media.Review;
using Avayomi.Core.AniList.Models.Media.Schedule;
using Avayomi.Core.AniList.Models.Media.Trend;
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
    /// <summary>
    /// Gets a collection of supported genres.
    /// </summary>
    public async Task<string[]> GetGenreCollectionAsync(
        CancellationToken cancellationToken = default
    )
    {
        var selections = new GqlSelection("GenreCollection");
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return GqlParser.ParseFromJson<string[]>(response["GenreCollection"]) ?? [];
    }

    /// <summary>
    /// Gets a collection of supported tags.
    /// </summary>
    public async Task<MediaTag[]> GetTagCollectionAsync(
        CancellationToken cancellationToken = default
    )
    {
        var selections = new GqlSelection("MediaTagCollection")
        {
            Selections = GqlParser.ParseToSelections<MediaTag>().ToArray(),
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return GqlParser.ParseFromJson<MediaTag[]>(response["MediaTagCollection"]) ?? [];
    }

    /// <summary>
    /// Gets the media with the given ID.
    /// </summary>
    public async Task<Media> GetMediaAsync(
        int mediaId,
        CancellationToken cancellationToken = default
    )
    {
        var selections = new GqlSelection("Media")
        {
            Parameters = [new GqlParameter("id", mediaId)],
            Selections = GqlParser.ParseToSelections<Media>().ToArray(),
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return GqlParser.ParseFromJson<Media>(response["Media"])!;
    }

    /// <summary>
    /// Gets the review with the given ID.
    /// </summary>
    public async Task<MediaReview> GetMediaReviewAsync(
        int reviewId,
        CancellationToken cancellationToken = default
    )
    {
        var selections = new GqlSelection("Review")
        {
            Parameters = [new GqlParameter("id", reviewId)],
            Selections = GqlParser.ParseToSelections<MediaReview>().ToArray(),
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return GqlParser.ParseFromJson<MediaReview>(response["Review"])!;
    }

    /// <summary>
    /// Gets collection of media schedules.
    /// </summary>
    public async Task<AniListPagination<MediaSchedule>> GetMediaSchedulesAsync(
        MediaSchedulesFilter? filter = null,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        filter ??= new MediaSchedulesFilter();
        paginationFilter ??= new AniListPaginationFilter();
        var selections = new GqlSelection("Page")
        {
            Parameters = paginationFilter.ToParameters(),
            Selections =
            [
                new GqlSelection("pageInfo", GqlParser.ParseToSelections<PageInfo>()),
                new GqlSelection(
                    "airingSchedules",
                    GqlParser.ParseToSelections<MediaSchedule>(),
                    filter.ToParameters()
                ),
            ],
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return new AniListPagination<MediaSchedule>(
            GqlParser.ParseFromJson<PageInfo>(response["Page"]!["pageInfo"])!,
            GqlParser.ParseFromJson<MediaSchedule[]>(response["Page"]!["airingSchedules"])!
        );
    }

    public Task<AniListPagination<MediaSchedule>> GetMediaSchedulesAsync(
        Action<MediaSchedulesFilter> configureFilter,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        var filter = new MediaSchedulesFilter();
        configureFilter(filter);
        return GetMediaSchedulesAsync(filter, paginationFilter, cancellationToken);
    }

    /// <summary>
    /// Gets collection of trending media.
    /// </summary>
    public async Task<AniListPagination<MediaTrend>> GetTrendingMediaAsync(
        MediaTrendFilter? filter = null,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        filter ??= new MediaTrendFilter();
        paginationFilter ??= new AniListPaginationFilter();
        var selections = new GqlSelection("Page")
        {
            Parameters = paginationFilter.ToParameters(),
            Selections =
            [
                new GqlSelection("pageInfo", GqlParser.ParseToSelections<PageInfo>()),
                new GqlSelection(
                    "mediaTrends",
                    GqlParser.ParseToSelections<MediaTrend>(),
                    filter.ToParameters()
                ),
            ],
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return new AniListPagination<MediaTrend>(
            GqlParser.ParseFromJson<PageInfo>(response["Page"]!["pageInfo"])!,
            GqlParser.ParseFromJson<MediaTrend[]>(response["Page"]!["mediaTrends"])!
        );
    }

    public Task<AniListPagination<MediaTrend>> GetTrendingMediaAsync(
        Action<MediaTrendFilter> configureFilter,
        AniListPaginationFilter? paginationFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        var filter = new MediaTrendFilter();
        configureFilter(filter);
        return GetTrendingMediaAsync(filter, paginationFilter, cancellationToken);
    }

    /// <summary>
    /// Gets the character with the given ID.
    /// </summary>
    public async Task<Character> GetCharacterAsync(
        int characterId,
        CancellationToken cancellationToken = default
    )
    {
        var selections = new GqlSelection("Character")
        {
            Parameters = [new GqlParameter("id", characterId)],
            Selections = GqlParser.ParseToSelections<Character>(),
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return GqlParser.ParseFromJson<Character>(response["Character"])!;
    }

    /// <summary>
    /// Gets the staff with the given ID.
    /// </summary>
    public async Task<Staff> GetStaffAsync(
        int staffId,
        CancellationToken cancellationToken = default
    )
    {
        var selections = new GqlSelection("Staff")
        {
            Parameters = [new GqlParameter("id", staffId)],
            Selections = GqlParser.ParseToSelections<Staff>(),
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return GqlParser.ParseFromJson<Staff>(response["Staff"])!;
    }

    /// <summary>
    /// Gets the studio with the given ID.
    /// </summary>
    public async Task<Studio> GetStudioAsync(
        int studioId,
        CancellationToken cancellationToken = default
    )
    {
        var selections = new GqlSelection("Studio")
        {
            Parameters = [new GqlParameter("id", studioId)],
            Selections = GqlParser.ParseToSelections<Studio>(),
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return GqlParser.ParseFromJson<Studio>(response["Studio"])!;
    }

    /// <summary>
    /// Gets the user with the given ID.
    /// </summary>
    public async Task<User> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var selections = new GqlSelection("User")
        {
            Parameters = [new GqlParameter("id", userId)],
            Selections = GqlParser.ParseToSelections<User>(),
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return GqlParser.ParseFromJson<User>(response["User"])!;
    }
}
