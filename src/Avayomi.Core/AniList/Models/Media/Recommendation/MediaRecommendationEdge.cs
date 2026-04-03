using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList.Models.Media.Recommendation;

public class MediaRecommendationEdge
{
    [GqlSelection("node")]
    public MediaRecommendation Recommendation { get; private set; }
}
