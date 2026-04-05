using AutoInterfaceAttributes;
using Avayomi.Core.AniList.Models.Media;
using Avayomi.Core.AniList.Models.Media.Entry;
using Avayomi.Core.AniList.Models.Media.Recommendation;
using Avayomi.Core.AniList.Models.Media.Review;
using Avayomi.Core.AniList.Models.User;
using Avayomi.Core.AniList.Parameters;
using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList;

[AutoInterface]
internal partial class AniListClient
{
    public async Task<User> GetAuthenticatedUserAsync(CancellationToken cancellationToken = default)
    {
        var selections = new GqlSelection("Viewer")
        {
            Selections = GqlParser.ParseToSelections<User>(),
        };
        var response = await PostRequestAsync(selections, cancellationToken: cancellationToken);
        return GqlParser.ParseFromJson<User>(response["Viewer"])!;
    }

    public async Task<User> UpdateUserOptionsAsync(
        UserOptionsMutation mutation,
        CancellationToken cancellationToken = default
    )
    {
        var selections = new GqlSelection("UpdateUser")
        {
            Parameters = mutation.ToParameters(),
            Selections = GqlParser.ParseToSelections<User>(),
        };
        var response = await PostRequestAsync(selections, true, cancellationToken);
        return GqlParser.ParseFromJson<User>(response["UpdateUser"])!;
    }

    public async Task<User> UpdateUserOptionsAsync(
        Action<UserOptionsMutation> configureMutation,
        CancellationToken cancellationToken = default
    )
    {
        var mutation = new UserOptionsMutation();
        configureMutation(mutation);
        var selections = new GqlSelection("UpdateUser")
        {
            Parameters = mutation.ToParameters(),
            Selections = GqlParser.ParseToSelections<User>(),
        };
        var response = await PostRequestAsync(selections, true, cancellationToken);
        return GqlParser.ParseFromJson<User>(response["UpdateUser"])!;
    }

    /// <summary>
    /// Create or update a media entry.
    /// </summary>
    public async Task<MediaEntry> SaveMediaEntryAsync(
        int mediaId,
        MediaEntryMutation mutation,
        CancellationToken cancellationToken = default
    )
    {
        var selections = new GqlSelection("SaveMediaListEntry")
        {
            Parameters = new GqlParameter[] { new("mediaId", mediaId) }
                .Concat(mutation.ToParameters())
                .ToArray(),
            Selections = GqlParser.ParseToSelections<MediaEntry>(),
        };
        var response = await PostRequestAsync(selections, true, cancellationToken);
        return GqlParser.ParseFromJson<MediaEntry>(response["SaveMediaListEntry"])!;
    }

    /// <summary>
    /// Create or update a media entry.
    /// </summary>
    public async Task<MediaEntry> SaveMediaEntryAsync(
        int mediaId,
        Action<MediaEntryMutation> configureMutation,
        CancellationToken cancellationToken = default
    )
    {
        var mutation = new MediaEntryMutation();
        configureMutation(mutation);
        var selections = new GqlSelection("SaveMediaListEntry")
        {
            Parameters = new GqlParameter[] { new("mediaId", mediaId) }
                .Concat(mutation.ToParameters())
                .ToArray(),
            Selections = GqlParser.ParseToSelections<MediaEntry>(),
        };
        var response = await PostRequestAsync(selections, true, cancellationToken);
        return GqlParser.ParseFromJson<MediaEntry>(response["SaveMediaListEntry"])!;
    }

    /// <summary>
    /// Delete a media entry.
    /// </summary>
    public async Task<bool> DeleteMediaEntryAsync(
        int mediaId,
        CancellationToken cancellationToken = default
    )
    {
        var selections = new GqlSelection(
            "DeleteMediaListEntry",
            [new GqlSelection("deleted")],
            [new GqlParameter("id", mediaId)]
        );
        var response = await PostRequestAsync(selections, true, cancellationToken);
        return GqlParser.ParseFromJson<bool>(response["DeleteMediaListEntry"]!["deleted"]);
    }

    /// <summary>
    /// Create or update a review.
    /// </summary>
    public async Task<MediaReview> SaveMediaReviewAsync(
        int mediaId,
        MediaReviewMutation mutation,
        CancellationToken cancellationToken = default
    )
    {
        var parameters = new List<GqlParameter> { new("mediaId", mediaId) }.Concat(
            mutation.ToParameters()
        );
        var selections = new GqlSelection(
            "SaveReview",
            GqlParser.ParseToSelections<MediaReview>(),
            parameters.ToArray()
        );
        var response = await PostRequestAsync(selections, true, cancellationToken);
        return GqlParser.ParseFromJson<MediaReview>(response["SaveReview"])!;
    }

    /// <summary>
    /// Create or update a review.
    /// </summary>
    public async Task<MediaReview> SaveMediaReviewAsync(
        int mediaId,
        Action<MediaReviewMutation> configureMutation,
        CancellationToken cancellationToken = default
    )
    {
        var mutation = new MediaReviewMutation();
        configureMutation(mutation);
        var parameters = new List<GqlParameter> { new("mediaId", mediaId) }.Concat(
            mutation.ToParameters()
        );
        var selections = new GqlSelection(
            "SaveReview",
            GqlParser.ParseToSelections<MediaReview>(),
            parameters.ToArray()
        );
        var response = await PostRequestAsync(selections, true, cancellationToken);
        return GqlParser.ParseFromJson<MediaReview>(response["SaveReview"])!;
    }

    /// <summary>
    /// Delete a review.
    /// </summary>
    public async Task<bool> DeleteMediaReviewAsync(
        int reviewId,
        CancellationToken cancellationToken = default
    )
    {
        var selections = new GqlSelection(
            "DeleteReview",
            [new GqlSelection("deleted")],
            [new GqlParameter("id", reviewId)]
        );
        var response = await PostRequestAsync(selections, true, cancellationToken);
        return GqlParser.ParseFromJson<bool>(response["DeleteReview"]!["deleted"]);
    }

    /// <summary>
    /// Rate a review.
    /// </summary>
    public async Task<MediaReview> RateMediaReviewAsync(
        int reviewId,
        MediaReviewRating rating,
        CancellationToken cancellationToken = default
    )
    {
        var parameters = new List<GqlParameter>
        {
            new("reviewId", reviewId),
            new("rating", rating),
        };
        var selections = new GqlSelection(
            "RateReview",
            GqlParser.ParseToSelections<MediaReview>(),
            parameters.ToArray()
        );
        var response = await PostRequestAsync(selections, true, cancellationToken);
        return GqlParser.ParseFromJson<MediaReview>(response["RateReview"])!;
    }

    /// <summary>
    /// Save a recommendation on a media.
    /// </summary>
    public async Task<MediaRecommendation> SaveMediaRecommendationAsync(
        int mediaId,
        MediaRecommendationMutation mutation,
        CancellationToken cancellationToken = default
    )
    {
        var parameters = new List<GqlParameter> { new("mediaId", mediaId) }.Concat(
            mutation.ToParameters()
        );
        var selections = new GqlSelection(
            "SaveRecommendation",
            GqlParser.ParseToSelections<MediaRecommendation>(),
            parameters.ToArray()
        );
        var response = await PostRequestAsync(selections, true, cancellationToken);
        return GqlParser.ParseFromJson<MediaRecommendation>(response["SaveRecommendation"])!;
    }

    /// <summary>
    /// Save a recommendation on a media.
    /// </summary>
    public async Task<MediaRecommendation> SaveMediaRecommendationAsync(
        int mediaId,
        Action<MediaRecommendationMutation> configureMutation,
        CancellationToken cancellationToken = default
    )
    {
        var mutation = new MediaRecommendationMutation();
        configureMutation(mutation);
        var parameters = new List<GqlParameter> { new("mediaId", mediaId) }.Concat(
            mutation.ToParameters()
        );
        var selections = new GqlSelection(
            "SaveRecommendation",
            GqlParser.ParseToSelections<MediaRecommendation>(),
            parameters.ToArray()
        );
        var response = await PostRequestAsync(selections, true, cancellationToken);
        return GqlParser.ParseFromJson<MediaRecommendation>(response["SaveRecommendation"])!;
    }

    public async Task<bool> ToggleFollowUserAsync(
        int userId,
        CancellationToken cancellationToken = default
    )
    {
        var selections = new GqlSelection(
            "ToggleFollow",
            [new GqlSelection("isFollowing")],
            [new GqlParameter("userId", userId)]
        );
        var response = await PostRequestAsync(selections, true, cancellationToken);
        return GqlParser.ParseFromJson<bool>(response["ToggleFollow"]!["isFollowing"]);
    }

    public async Task<bool> ToggleMediaFavoriteAsync(
        int mediaId,
        MediaType type,
        CancellationToken cancellationToken = default
    )
    {
        await ToggleFavoriteAsync(
            type switch
            {
                MediaType.Anime => "animeId",
                MediaType.Manga => "mangaId",
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            },
            mediaId,
            cancellationToken
        );
        var json = await GetSingleDataAsync(
            [
                new GqlSelection("Media", null, [new GqlParameter("id", mediaId)]),
                new GqlSelection("isFavourite"),
            ],
            cancellationToken
        );
        return GqlParser.ParseFromJson<bool>(json);
    }

    public async Task<bool> ToggleCharacterFavoriteAsync(
        int characterId,
        CancellationToken cancellationToken = default
    )
    {
        await ToggleFavoriteAsync("characterId", characterId, cancellationToken);
        var json = await GetSingleDataAsync(
            [
                new GqlSelection("Character", null, [new GqlParameter("id", characterId)]),
                new GqlSelection("isFavourite"),
            ],
            cancellationToken
        );
        return GqlParser.ParseFromJson<bool>(json);
    }

    public async Task<bool> ToggleStaffFavoriteAsync(
        int staffId,
        CancellationToken cancellationToken = default
    )
    {
        await ToggleFavoriteAsync("staffId", staffId, cancellationToken);
        var json = await GetSingleDataAsync(
            [
                new GqlSelection("Staff", null, [new GqlParameter("id", staffId)]),
                new GqlSelection("isFavourite"),
            ],
            cancellationToken
        );
        return GqlParser.ParseFromJson<bool>(json);
    }

    public async Task<bool> ToggleStudioFavoriteAsync(
        int studioId,
        CancellationToken cancellationToken = default
    )
    {
        await ToggleFavoriteAsync("studioId", studioId, cancellationToken);
        var json = await GetSingleDataAsync(
            [
                new GqlSelection("Studio", null, [new GqlParameter("id", studioId)]),
                new GqlSelection("isFavourite"),
            ],
            cancellationToken
        );
        return GqlParser.ParseFromJson<bool>(json);
    }

    /* below are methods made for private use */

    private async Task ToggleFavoriteAsync(
        string field,
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var selections = new GqlSelection(
            "ToggleFavourite",
            [
                new GqlSelection(
                    "anime",
                    [new GqlSelection("pageInfo", [new GqlSelection("total")])]
                ),
            ],
            [new GqlParameter(field, id)]
        );
        _ = await PostRequestAsync(selections, true, cancellationToken);
    }
}
