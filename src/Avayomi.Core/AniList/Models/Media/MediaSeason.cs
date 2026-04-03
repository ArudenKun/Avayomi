using System.Runtime.Serialization;

namespace Avayomi.Core.AniList.Models.Media;

public enum MediaSeason
{
    [EnumMember(Value = "WINTER")]
    Winter,

    [EnumMember(Value = "SPRING")]
    Spring,

    [EnumMember(Value = "SUMMER")]
    Summer,

    [EnumMember(Value = "FALL")]
    Fall,
}
