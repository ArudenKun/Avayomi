using System.Runtime.Serialization;

namespace Avayomi.Core.AniList.Models.Media.Entry;

public enum MediaEntryStatus
{
    [EnumMember(Value = "CURRENT")]
    Current,

    [EnumMember(Value = "PLANNING")]
    Planning,

    [EnumMember(Value = "COMPLETED")]
    Completed,

    [EnumMember(Value = "DROPPED")]
    Dropped,

    [EnumMember(Value = "PAUSED")]
    Paused,

    [EnumMember(Value = "REPEATING")]
    Repeating,
}
