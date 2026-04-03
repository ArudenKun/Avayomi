using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList.Models.Media.Edges;

public class MediaCharacterEdge : MediaEdge
{
    /// <summary>
    /// The character's role in the media.
    /// </summary>
    [GqlSelection("characterRole")]
    public string Role { get; private set; }
}
