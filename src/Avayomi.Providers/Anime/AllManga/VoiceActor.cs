using Avayomi.Core.GraphQL;

namespace Avayomi.Providers.Anime.AllManga;

internal class VoiceActor
{
    [GqlSelection("language")]
    public string Language { get; private set; } = string.Empty;

    [GqlSelection("aniListId")]
    public int AniListId { get; private set; }
}
