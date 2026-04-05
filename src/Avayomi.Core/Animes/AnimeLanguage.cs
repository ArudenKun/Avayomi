namespace Avayomi.Core.Animes;

using System;

/// <summary>
/// Used to select sub, dub, or both for anime.
/// </summary>
[Flags]
public enum AnimeLanguage
{
    /// <summary>
    /// Selects sub.
    /// </summary>
    Sub = 1 << 0, // 1

    /// <summary>
    /// Selects dub.
    /// </summary>
    Dub = 1 << 1, // 2

    /// <summary>
    /// Selects all available (sub and dub).
    /// This is the default value (0).
    /// </summary>
    All = Sub | Dub,
}
