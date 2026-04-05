using Avayomi.Core.Animes;

namespace Avayomi.Core.Trackers;

public interface ITracker
{
    Task<IReadOnlyList<TrackerInfoResult>> SearchAsync(
        string query,
        int page = 1,
        int perPage = 20,
        CancellationToken cancellationToken = default
    );

    Task<TrackerInfoResult?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrackerInfoResult>> GetUserListAsync(
        AnimeStatus status = AnimeStatus.All,
        CancellationToken cancellationToken = default
    );

    Task<TrackerInfoResult?> UpdateAsync(
        string id,
        AnimeStatus status = AnimeStatus.All,
        int? score = null,
        int? progress = null,
        int? episodesWatched = null,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
