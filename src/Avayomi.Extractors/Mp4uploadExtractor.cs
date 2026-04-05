using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public class Mp4UploadExtractor : VideoExtractorBase
{
    public Mp4UploadExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override string ServerName => "Mp4upload";

    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateClient();

        headers = new Dictionary<string, string>() { ["Referer"] = "https://mp4upload.com/" };

        var response = await http.ExecuteAsync(url, headers, cancellationToken);

        var document = HtmlHelper.Parse(response);

        // 1. Find the script containing the 'src: ' link
        var scriptElement = document
            .QuerySelectorAll("script")
            .FirstOrDefault(x => x.InnerHtml.Contains("src: "));

        var link = scriptElement?.InnerHtml.SubstringAfter("src: \"").SubstringBefore("\"");

        if (!string.IsNullOrWhiteSpace(link))
        {
            // Extracting host: link is "https://example.com/path" -> result "example.com"
            var host = link.SubstringAfter("https://").SubstringBefore("/");
            headers.Add("host", host);

            return
            [
                new VideoSource
                {
                    Format = VideoType.Container,
                    VideoUrl = link,
                    Resolution = "Default Quality",
                    Headers = headers,
                },
            ];
        }

        // 2. If not found, look for the 'packed' eval script
        // We target a script that contains the signature 'eval(function(p,a,c,k,e,d)'
        var packedScript = document
            .QuerySelectorAll("script")
            .FirstOrDefault(x => x.InnerHtml.Contains("eval(function(p,a,c,k,e,d)"))
            ?.InnerHtml;

        // Since we have the node directly, we don't need to split by </script> anymore.
        // We just need to ensure we grab the part starting from 'eval'
        var packed = packedScript?.SubstringAfter("eval(function(p,a,c,k,e,d)");

        var unpacked = JavaScriptUnpacker.UnpackAndCombine($"eval(function(p,a,c,k,e,d){packed}");

        if (string.IsNullOrEmpty(unpacked))
            return [];

        var videoUrl = unpacked
            .SubstringAfter("player.src(\"")
            .Split(["\");"], StringSplitOptions.None)[0];

        return
        [
            new VideoSource
            {
                Format = VideoType.Container,
                VideoUrl = videoUrl,
                Resolution = "Default Quality",
                Headers = headers,
            },
        ];
    }
}
