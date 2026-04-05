using Avayomi.Core.Anime;

namespace Avayomi.Core.Providers.Anime;

/// <summary>
/// Interface for basic operations related to last updated sources.
/// </summary>
public interface ILastUpdatedProvider
{
    /// <summary>
    /// Search for anime.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> of <see cref="AnimeInfo"/>s.</returns>
    ValueTask<List<AnimeInfo>> GetLastUpdatedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    );
}
