using System.Text.Json.Serialization;

namespace Core.Suwayomi;

public class Manga
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("artist")]
    public string Artist { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("genre")]
    public List<string> Genre { get; set; } = [];

    [JsonPropertyName("inLibrary")]
    public bool InLibrary { get; set; }

    [JsonPropertyName("realUrl")]
    public string RealUrl { get; set; } = "";

    [JsonPropertyName("thumbnailUrl")]
    public string ThumbnailUrl { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
}
