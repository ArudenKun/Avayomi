namespace Avayomi.Core.Animes;

/// <summary>
/// Represents the various title formats for an anime.
/// </summary>
/// <param name="Romaji">The romanization of the native language title.</param>
/// <param name="English">The official english title.</param>
/// <param name="Native">Official title in its native language.</param>
/// <param name="UserPreferred">The users preferred title language (defaults to Romaji).</param>
public sealed record AnimeNames(
    string Romaji,
    string Native = "",
    string UserPreferred = "",
    string English = ""
)
{
    public IReadOnlyList<string> Names => [Romaji, Native, UserPreferred, English];
}
