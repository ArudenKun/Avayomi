using Avayomi.Core.AniList.Models.Media.Entry;
using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList.Models.Media.List;

public class MediaList
{
    [GqlSelection("name")]
    public string Name { get; private set; }

    [GqlSelection("isCustomList")]
    public string IsCustom { get; private set; }

    [GqlSelection("status")]
    public MediaEntryStatus? Status { get; private set; }
}
