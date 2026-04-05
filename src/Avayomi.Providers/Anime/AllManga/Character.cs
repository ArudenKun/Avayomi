using Avayomi.Core.GraphQL;

namespace Avayomi.Providers.Anime.AllManga;

internal class Character
{
    [GqlSelection("role")]
    public string Role { get; private set; }

    [GqlSelection("name")]
    public Name Name { get; private set; }

    [GqlSelection("image")]
    public Image Image { get; private set; }

    [GqlSelection("aniListId")]
    public int AniListId { get; private set; }

    [GqlSelection("voiceActors")]
    public IReadOnlyList<VoiceActor> VoiceActors { get; private set; }
}
