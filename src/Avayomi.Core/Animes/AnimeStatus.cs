using System.Text.Json.Serialization;

namespace Avayomi.Core.Animes;

using System;

/// <summary>
/// Bitwise flags representing the status of an anime.
/// Useful for multi-status filtering.
/// </summary>
[Flags]
public enum AnimeStatus
{
    // We omit 'None' and start at 1 (2^0)
    [JsonStringEnumMemberName("CURRENT")]
    Watching = 1 << 0, // 1

    [JsonStringEnumMemberName("COMPLETED")]
    Completed = 1 << 1, // 2

    [JsonStringEnumMemberName("PAUSED")]
    OnHold = 1 << 2, // 4

    [JsonStringEnumMemberName("DROPPED")]
    Dropped = 1 << 3, // 8

    [JsonStringEnumMemberName("PLANNING")]
    PlanToWatch = 1 << 4, // 16

    /// <summary>
    /// Combined flag for convenience.
    /// </summary>
    All = Watching | Completed | OnHold | Dropped | PlanToWatch,
}
