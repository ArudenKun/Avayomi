using System;

namespace Avayomi.Data.Caching;

/// <summary>
/// Options for controlling a <see cref="FileCache"/>.
/// </summary>
/// <param name="DirectoryPath">The directory to store the cache in.</param>
/// <param name="RemoveExpiredInterval">The time interval controlling how often the cache is removed of expired entries.</param>
/// <param name="ManifestSaveInterval">The time interval controlling how often the cache manifest is saved to disk.</param>
public record FileCacheOptions(
    string DirectoryPath,
    TimeSpan? RemoveExpiredInterval = null,
    TimeSpan? ManifestSaveInterval = null
)
{
    /// <summary>
    /// The default remove expired interval of 30 minutes.
    /// </summary>
    public TimeSpan DefaultRemovedExpiredInterval { get; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// The default manifest save interval of 30 seconds.
    /// </summary>
    public TimeSpan DefaultManifestSaveInterval { get; } = TimeSpan.FromSeconds(30);
}
