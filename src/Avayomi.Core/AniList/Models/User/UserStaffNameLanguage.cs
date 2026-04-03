using System.Runtime.Serialization;

namespace Avayomi.Core.AniList.Models.User;

public enum UserStaffNameLanguage
{
    [EnumMember(Value = "ROMAJI_WESTERN")]
    WesternRomaji,

    [EnumMember(Value = "ROMAJI")]
    Romaji,

    [EnumMember(Value = "NATIVE")]
    Native,
}
