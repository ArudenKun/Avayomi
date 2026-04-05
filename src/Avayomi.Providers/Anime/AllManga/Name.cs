using Avayomi.Core.GraphQL;

namespace Avayomi.Providers.Anime.AllManga;

internal class Name
{
    [GqlSelection("full")]
    public string Full { get; private set; }

    [GqlSelection("native")]
    public string Native { get; private set; }
}
