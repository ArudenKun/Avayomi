using Avayomi.Core.Animes;

namespace Avayomi.Core.Providers.Anime;

/// <summary>
/// Interface for basic operations related to popular sources.
/// </summary>
public interface IPopularProvider
{
    /// <summary>
    /// Search for anime.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="List{T}"/> of <see cref="AnimeInfo"/>s.</returns>
    ValueTask<List<AnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    );
}
