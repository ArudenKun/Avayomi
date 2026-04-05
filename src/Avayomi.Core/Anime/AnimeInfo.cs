namespace Avayomi.Core.Anime;

/// <summary>
/// The Class which contains all the information about an Anime
/// </summary>
public class AnimeInfo : IAnimeInfo
{
    public AnimeInfo(string id)
    {
        Id = id;
    }

    public string Id { get; set; }

    public string AniListId { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int Episodes { get; set; }

    public string? Released { get; set; }

    public string? Category { get; set; }

    public string? Link { get; set; }

    public string? Image { get; set; }

    public string? Type { get; set; }

    public string? Status { get; set; }

    public string? OtherNames { get; set; }

    public string? Summary { get; set; }

    public List<Genre> Genres { get; set; } = [];

    public Dictionary<string, object> Metadata { get; set; } = [];

    public override string ToString() => $"{Title}";
}
