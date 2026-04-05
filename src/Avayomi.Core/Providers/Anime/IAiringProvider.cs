using Avayomi.Core.Animes;

namespace Avayomi.Core.Providers.Anime;

/// <summary>
/// Interface for basic operations related to airing sources.
/// </summary>
public interface IAiringProvider
{
    /// <summary>
    /// Search for anime.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> of <see cref="AnimeInfo"/>s.</returns>
    ValueTask<List<AnimeInfo>> GetAiringAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    );
}
