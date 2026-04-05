using Avayomi.Core.Animes;

namespace Avayomi.Core.Providers.Anime;

/// <summary>
/// Interface for basic operations related to recently added sources.
/// </summary>
public interface IRecentlyAddedProvider
{
    /// <summary>
    /// Search for anime.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> of <see cref="AnimeInfo"/>s.</returns>
    ValueTask<List<AnimeInfo>> GetRecentlyAddedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    );
}
