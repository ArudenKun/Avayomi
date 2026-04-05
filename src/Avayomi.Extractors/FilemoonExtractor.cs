using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public class FilemoonExtractor : VideoExtractorBase
{
    public FilemoonExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override string ServerName => "Filemoon";

    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateProviderHttpClient();

        var response = await http.ExecuteAsync(url, cancellationToken);

        var document = HtmlHelper.Parse(response);

        var scriptNode = document
            .QuerySelectorAll("script")
            .FirstOrDefault(x => x.InnerHtml.Contains("eval"));

        var unpacked = JavaScriptUnpacker.UnpackAndCombine(scriptNode?.TextContent);

        var masterUrl = unpacked.SubstringAfter("{file:\"").Split(["\"}"], StringSplitOptions.None)[
            0
        ];

        return
        [
            new VideoSource
            {
                Format = VideoType.M3U8,
                VideoUrl = masterUrl,
                Resolution = "Multi Quality",
            },
        ];
    }
}
