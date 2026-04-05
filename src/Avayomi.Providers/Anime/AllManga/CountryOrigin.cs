using System.Runtime.Serialization;

namespace Avayomi.Providers.Anime.AllManga;

internal enum CountryOrigin
{
    [EnumMember(Value = "ALL")]
    All,

    [EnumMember(Value = "CN")]
    China,

    [EnumMember(Value = "JP")]
    Japan,

    [EnumMember(Value = "KR")]
    Korea,

    [EnumMember(Value = "OTHER")]
    Other,
}
