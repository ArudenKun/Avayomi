using System.Runtime.Serialization;

namespace Avayomi.Core.AniList.Models.Media;

public enum MediaType
{
    /// <summary>
    /// Japanese Anime.
    /// </summary>
    [EnumMember(Value = "ANIME")]
    Anime,

    /// <summary>
    /// Asian comic.
    /// </summary>
    [EnumMember(Value = "MANGA")]
    Manga,
}
