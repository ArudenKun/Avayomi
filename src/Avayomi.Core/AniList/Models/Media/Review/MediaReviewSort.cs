using System.Runtime.Serialization;

namespace Avayomi.Core.AniList.Models.Media.Review;

public enum MediaReviewSort
{
    [EnumMember(Value = "ID")]
    Id,

    [EnumMember(Value = "SCORE")]
    Score,

    [EnumMember(Value = "RATING")]
    Rating,

    [EnumMember(Value = "CREATED_AT")]
    CreatedAt,

    [EnumMember(Value = "UPDATED_AT")]
    UpdatedAt,
}
