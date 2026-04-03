using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList.Models.Media.Edges;

public class MediaRelationEdge : MediaEdge
{
    /// <summary>
    /// The type of relation to the parent model.
    /// </summary>
    [GqlSelection("relationType")]
    [GqlParameter("version", 2)]
    public MediaRelation Relation { get; private set; }
}
