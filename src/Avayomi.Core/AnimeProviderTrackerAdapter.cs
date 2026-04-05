using Avayomi.Core.Providers.Anime;
using Avayomi.Core.Trackers;

namespace Avayomi.Core;

public class AnimeProviderTrackerAdapter
{
    private readonly IAnimeProvider _animeProvider;
    private readonly ITracker _tracker;

    public AnimeProviderTrackerAdapter(IAnimeProvider animeProvider, ITracker tracker)
    {
        _animeProvider = animeProvider;
        _tracker = tracker;
    }
}
