namespace Avayomi.Core.Animes;

/// <summary>
/// The Class which contains all the information about an Anime
/// </summary>
public sealed record AnimeInfo(
    string Id,
    string Title,
    IReadOnlyList<string> AlternativeTitles,
    string Description,
    AnimeImages Images,
    AnimeStatus Status,
    AnimeUserStatus UserStatus,
    int Episodes,
    int Popularity,
    IReadOnlyList<string> Studios,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<string> Genres,
    string TrailerUrl
)
{
    public static readonly AnimeInfo Empty = new(
        string.Empty,
        string.Empty,
        [],
        string.Empty,
        new AnimeImages(string.Empty, string.Empty, string.Empty),
        AnimeStatus.All,
        new AnimeUserStatus(AnimeStatus.All, 0, 0),
        0,
        0,
        [],
        DateOnly.MinValue,
        DateOnly.MinValue,
        [],
        string.Empty
    );
}
