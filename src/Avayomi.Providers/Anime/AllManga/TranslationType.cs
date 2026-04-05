using System.Runtime.Serialization;

namespace Avayomi.Providers.Anime.AllManga;

internal enum TranslationType
{
    [EnumMember(Value = "raw")]
    Raw,

    [EnumMember(Value = "sub")]
    Sub,

    [EnumMember(Value = "dub")]
    Dub,
}
