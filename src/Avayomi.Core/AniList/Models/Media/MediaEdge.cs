using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList.Models.Media;

public class MediaEdge
{
    /// <summary>
    /// The media of the connection.
    /// </summary>
    [GqlSelection("node")]
    public Media Media { get; private set; }

    /// <summary>
    /// The ID of the connection.
    /// </summary>
    [GqlSelection("id")]
    public string Id { get; private set; }
}
