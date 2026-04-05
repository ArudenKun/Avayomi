using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Avayomi.Core.Trackers.AniList;

public enum AniListMediaType
{
    [EnumMember(Value = "ANIME")]
    [JsonStringEnumMemberName("ANIME")]
    Anime,
}
