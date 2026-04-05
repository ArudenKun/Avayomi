using System.Text.Json.Serialization;
using Avayomi.Core.Animes;

namespace Avayomi.Core.Trackers;

public sealed record TrackerInfoResult(
    string Id,
    int EpisodeCount,
    [property: JsonPropertyName("title")] AnimeNames Names,
    DateOnly StartDate,
    DateOnly EndDate,
    AnimeUserList UserList
);
