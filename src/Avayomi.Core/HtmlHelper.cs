using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace Avayomi.Core;

public static class HtmlHelper
{
    public static IHtmlDocument Parse(string html)
    {
        var parser = new HtmlParser();
        var htmlDocument = parser.ParseDocument(html);
        return htmlDocument;
    }
}
