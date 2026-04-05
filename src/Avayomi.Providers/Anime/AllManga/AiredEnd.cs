using System.Text.Json.Serialization;

namespace Avayomi.Providers.Anime.AllManga;

internal class AiredEnd
{
    [JsonPropertyName("year")]
    public int Year { get; private set; }

    [JsonPropertyName("month")]
    public int Month { get; private set; }

    [JsonPropertyName("date")]
    public int Date { get; private set; }
}
