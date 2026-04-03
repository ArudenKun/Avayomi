using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList.Models.Studio;

public class StudioEdge
{
    [GqlSelection("node")]
    public Studio Studio { get; private set; }

    [GqlSelection("id")]
    public int Id { get; private set; }

    [GqlSelection("isMain")]
    public bool IsMain { get; private set; }
}
