using Avayomi.Core.GraphQL;

namespace Avayomi.Core.AniList.Models.Character;

public class CharacterEdge
{
    [GqlSelection("node")]
    public Character Character { get; private set; }

    [GqlSelection("id")]
    public int Id { get; private set; }

    [GqlSelection("role")]
    public CharacterRole Role { get; private set; }
}
