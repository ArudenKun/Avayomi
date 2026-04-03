using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList.Models.Media.Edges;

public class MediaStaffEdge : MediaEdge
{
    /// <summary>
    /// The role of the staff member in the production of the media.
    /// </summary>
    [GqlSelection("staffRole")]
    public string Role { get; private set; }
}
