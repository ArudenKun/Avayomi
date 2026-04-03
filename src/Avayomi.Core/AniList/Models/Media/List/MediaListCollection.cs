using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList.Models.Media.List;

public class MediaListCollection
{
    /// <summary>
    /// Grouped media list entries.
    /// </summary>
    [GqlSelection("lists")]
    public MediaList[] Lists { get; private set; }

    /// <summary>
    /// The owner of the list.
    /// </summary>
    [GqlSelection("user")]
    public User.User User { get; private set; }

    /// <summary>
    /// If there is another chunk.
    /// </summary>
    [GqlSelection("hasNextChunk")]
    public bool HasNextChunk { get; private set; }
}
