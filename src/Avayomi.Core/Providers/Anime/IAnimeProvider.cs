using Avayomi.Core.Anime;
using Avayomi.Core.Videos;

namespace Avayomi.Core.Providers.Anime;

/// <summary>
/// Interface for basic operations related to an anime provider.
/// </summary>
public interface IAnimeProvider : ISourceProvider, IVideoExtractorProvider, IKey
{
    /// <summary>
    /// If dub is available separately for the provider.
    /// </summary>
    public bool IsDubAvailableSeparately { get; }

    /// <summary>
    /// Search for anime.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="List{T}"/> of <see cref="AnimeInfo"/>s.</returns>
    ValueTask<List<AnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the anime info by Id.
    /// </summary>
    /// <param name="animeId">The anime Id.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An instance of <see cref="AnimeInfo"/> for the provider.</returns>
    ValueTask<AnimeInfo> GetAnimeInfoAsync(
        string animeId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets episodes for anime.
    /// </summary>
    /// <param name="animeId">The anime Id.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="List{T}"/> of <see cref="Episode"/>s.</returns>
    ValueTask<List<Episode>> GetEpisodesAsync(
        string animeId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets video servers for episode.
    /// </summary>
    /// <param name="episodeId">The episode Id.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="List{T}"/> of <see cref="VideoServer"/>s.</returns>
    ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    );
}
