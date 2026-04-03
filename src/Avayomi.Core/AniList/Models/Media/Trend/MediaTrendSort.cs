using System.Runtime.Serialization;

namespace Avayomi.Core.AniList.Models.Media.Trend;

public enum MediaTrendSort
{
    [EnumMember(Value = "ID")]
    Id,

    [EnumMember(Value = "MEDIA_ID")]
    MediaId,

    [EnumMember(Value = "DATE")]
    Date,

    [EnumMember(Value = "SCORE")]
    Score,

    [EnumMember(Value = "POPULARITY")]
    Popularity,

    [EnumMember(Value = "TRENDING")]
    Trending,

    [EnumMember(Value = "EPISODE")]
    Episode,
}
