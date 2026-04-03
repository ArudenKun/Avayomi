using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList.Models.Media.Entry;

public class MediaEntryCollection
{
    [GqlSelection("lists")]
    public MediaEntryList[] Lists { get; private set; }

    [GqlSelection("hasNextChunk")]
    public bool HasNextChunk { get; private set; }
}
