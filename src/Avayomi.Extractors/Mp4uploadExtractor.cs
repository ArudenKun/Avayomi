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

        var link = document
            .DocumentNode.Descendants()
            .Where(x => x.Name == "script")
            .FirstOrDefault(x => x.InnerText.Contains("src: "))
            ?.InnerText.SubstringAfter("src: \"")
            .SubstringBefore("\"");
        if (!string.IsNullOrWhiteSpace(link))
        {
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

        var packed = response
            .SubstringAfter("eval(function(p,a,c,k,e,d)")
            .Split(["</script>"], StringSplitOptions.None)[0];

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
