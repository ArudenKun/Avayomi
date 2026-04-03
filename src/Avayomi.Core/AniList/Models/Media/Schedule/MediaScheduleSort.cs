using System.Runtime.Serialization;

namespace Avayomi.Core.AniList.Models.Media.Schedule;

public enum MediaScheduleSort
{
    [EnumMember(Value = "ID")]
    Id,

    [EnumMember(Value = "MEDIA_ID")]
    MediaId,

    [EnumMember(Value = "TIME")]
    Time,

    [EnumMember(Value = "EPISODE")]
    Episode,
}
