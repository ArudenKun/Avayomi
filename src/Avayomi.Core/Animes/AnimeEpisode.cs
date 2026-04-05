namespace Avayomi.Core.Animes;

/// <summary>
/// The Class which contains all the information about an Episode
/// </summary>
public class AnimeEpisode
{
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public string? Description { get; set; }

    public float Number { get; set; }

    public float Duration { get; set; }

    public string Link { get; set; } = string.Empty;

    public string Thumbnail { get; set; } = string.Empty;

    public float Progress { get; set; }

    public override string ToString() => $"Episode {Number}";
}
