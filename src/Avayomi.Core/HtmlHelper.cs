using HtmlAgilityPack;

namespace Avayomi.Core;

public static class HtmlHelper
{
    public static HtmlDocument Parse(string source)
    {
        var document = new HtmlDocument();
        document.LoadHtml(HtmlEntity.DeEntitize(source) ?? string.Empty);
        return document;
    }
}
