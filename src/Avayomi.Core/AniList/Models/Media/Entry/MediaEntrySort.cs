using System.Runtime.Serialization;

namespace Avayomi.Core.AniList.Models.Media.Entry;

public enum MediaEntrySort
{
    [EnumMember(Value = "SCORE")]
    Score,

    [EnumMember(Value = "STATUS")]
    Status,

    [EnumMember(Value = "PROGRESS")]
    Progress,

    [EnumMember(Value = "PROGRESS_VOLUMES")]
    VolumeProgress,

    [EnumMember(Value = "REPEAT")]
    Repeat,

    [EnumMember(Value = "PRIORITY")]
    Priority,

    [EnumMember(Value = "UPDATED_TIME")]
    LastUpdated,

    [EnumMember(Value = "STARTED_ON")]
    StartedDate,

    [EnumMember(Value = "FINISHED_ON")]
    CompletedDate,

    [EnumMember(Value = "MEDIA_POPULARITY")]
    Popularity,
}
