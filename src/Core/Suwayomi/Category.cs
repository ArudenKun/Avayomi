using System.Text.Json.Serialization;

namespace Core.Suwayomi;

public class Category
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("default")]
    public bool Default { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("order")]
    public int Order { get; set; }
    public Manga[] Mangas { get; set; } = [];
}
