using System.Text.Json.Nodes;

namespace Avayomi.Core.GraphQL;

internal sealed class GqlSelection
{
    public string Name { get; set; }
    public string? Alias { get; set; }
    public IList<GqlParameter>? Parameters { get; set; }
    public IList<GqlSelection>? Selections { get; set; }

    public GqlSelection(string name)
    {
        Name = name;
    }

    public GqlSelection(
        string name,
        IEnumerable<GqlSelection>? selections = null,
        IEnumerable<GqlParameter>? parameters = null
    )
    {
        Name = name;
        Selections = selections?.ToList();
        Parameters = parameters?.ToList();
    }

    internal GqlSelection(GqlSelectionAttribute attribute)
    {
        Name = attribute.Name;
        Alias = attribute.Alias;
    }

    public string ToJsonString(bool isMutation = false)
    {
        var jsonObject = new JsonObject
        {
            ["query"] = (isMutation ? "mutation" : string.Empty) + this,
        };
        return jsonObject.ToJsonString();
    }

    public override string ToString()
    {
        return GqlParser.ParseToString(this);
    }
}
