namespace Avayomi.Core.Anime;

/// <summary>
/// The Class which contains all the information about an Anime
/// </summary>
public interface IAnimeInfo
{
    string Id { get; set; }

    string Title { get; set; }

    int Episodes { get; set; }

    string? Released { get; set; }

    string? Category { get; set; }

    string? Link { get; set; }

    string? Image { get; set; }

    string? Type { get; set; }

    string? Status { get; set; }

    string? OtherNames { get; set; }

    string? Summary { get; set; }

    List<Genre> Genres { get; set; }

    Dictionary<string, object> Metadata { get; set; }
}
