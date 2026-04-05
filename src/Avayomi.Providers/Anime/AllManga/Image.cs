using Avayomi.Core.GraphQL;

namespace Avayomi.Providers.Anime.AllManga;

internal class Image
{
    [GqlSelection("large")]
    public string Large { get; private set; }

    [GqlSelection("medium")]
    public string Medium { get; private set; }
}
