using Avayomi.Core.Anime;

namespace Avayomi.Core.Providers.Anime;

/// <summary>
/// Interface for basic operations related to recently added sources.
/// </summary>
public interface IRecentlyAddedProvider
{
    /// <summary>
    /// Search for anime.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> of <see cref="IAnimeInfo"/>s.</returns>
    ValueTask<List<IAnimeInfo>> GetRecentlyAddedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    );
}
