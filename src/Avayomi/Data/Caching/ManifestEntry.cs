using System;

namespace Avayomi.Data.Caching;

/// <summary>
/// The manifest entry for a file system based cache.
/// </summary>
/// <param name="FileName">The file name that contains the cached data.</param>
/// <param name="Expiry">The expiry date of the cached value.</param>
/// <param name="Renewal">The new expiry date of the cached value</param>
public readonly record struct ManifestEntry(string FileName, DateTimeOffset? Expiry, TimeSpan? Renewal);