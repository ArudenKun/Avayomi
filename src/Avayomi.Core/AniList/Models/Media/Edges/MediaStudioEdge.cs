using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList.Models.Media.Edges;

public class MediaStudioEdge : MediaEdge
{
    /// <summary>
    /// If the studio is the main animation studio of the media.
    /// </summary>
    [GqlSelection("isMainStudio")]
    public bool IsMainStudio { get; private set; }
}
