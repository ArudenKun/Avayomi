using Avayomi.Core.GraphQL;
using JetBrains.Annotations;

namespace Avayomi.Providers.Anime.AllManga;

[PublicAPI]
internal class AllMangaAnimeInfo
{
    [GqlSelection("_id")]
    public string Id { get; private set; } = string.Empty;

    [GqlSelection("malId")]
    public string MalId { get; private set; } = string.Empty;

    [GqlSelection("aniListId")]
    public string AniListId { get; private set; } = string.Empty;

    [GqlSelection("description")]
    public string Description { get; private set; } = string.Empty;

    [GqlSelection("name")]
    public string Name { get; private set; } = string.Empty;

    [GqlSelection("nativeName")]
    public string NativeName { get; private set; } = string.Empty;

    [GqlSelection("altNames")]
    public IReadOnlyList<string> AltNames { get; private set; } = [];

    [GqlSelection("englishName")]
    public string EnglishName { get; private set; } = string.Empty;

    [GqlSelection("trustedAltNames")]
    public IReadOnlyList<string> TrustedAltNames { get; private set; } = [];

    [GqlSelection("genres")]
    public IReadOnlyList<string> Genres { get; private set; } = [];

    [GqlSelection("availableEpisodes")]
    public AvailableEpisodes AvailableEpisodes { get; private set; } = new();

    [GqlSelection("score")]
    public double? Score { get; private set; }

    [GqlSelection("averageScore")]
    public int? AverageScore { get; private set; }

    [GqlSelection("banner")]
    public string Banner { get; private set; } = string.Empty;

    [GqlSelection("thumbnail")]
    public string Thumbnail { get; private set; } = string.Empty;

    [GqlSelection("episodeCount")]
    public string EpisodeCount { get; private set; } = "0";

    [GqlSelection("characters")]
    public IReadOnlyList<Character> Characters { get; private set; } = [];

    [GqlSelection("characterCount")]
    public string CharacterCount { get; private set; } = "0";

    [GqlSelection("status")]
    public string Status { get; private set; } = string.Empty;

    [GqlSelection("airedStart")]
    public AiredStart AiredStart { get; private set; } = new();

    [GqlSelection("airedEnd")]
    public AiredEnd AiredEnd { get; private set; } = new();
}
