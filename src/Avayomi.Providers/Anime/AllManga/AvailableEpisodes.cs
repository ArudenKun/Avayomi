using System.Text.Json.Serialization;

namespace Avayomi.Providers.Anime.AllManga;

internal class AvailableEpisodes
{
    [JsonPropertyName("sub")]
    public int Sub { get; private set; }

    [JsonPropertyName("dub")]
    public int Dub { get; private set; }

    [JsonPropertyName("raw")]
    public int Raw { get; private set; }
}
