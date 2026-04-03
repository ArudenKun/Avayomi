using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList.Models.Media.Trend;

public class MediaTrendEdge
{
    [GqlSelection("node")]
    public MediaTrend Node { get; private set; }
}
