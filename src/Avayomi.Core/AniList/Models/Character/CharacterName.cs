using Avayomi.Core.AniList.Models.Other;
using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList.Models.Character;

public class CharacterName : Name
{
    /// <summary>
    /// Other names the character might be referred to as but are spoilers.
    /// </summary>
    [GqlSelection("alternativeSpoiler")]
    public string[] AlternativeSpoilerNames { get; private set; }
}
