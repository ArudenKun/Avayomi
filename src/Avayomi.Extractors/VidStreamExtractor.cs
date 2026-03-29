using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public class VidStreamExtractor : VideoExtractorBase
{
    public VidStreamExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override string ServerName => "VidStream";

    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateClient();

        var response = await http.ExecuteAsync(url, cancellationToken);

        if (url.Contains("srcd"))
        {
            var link = response.FindBetween("\"file\": '", "',");

            return
            [
                new()
                {
                    Format = VideoType.M3U8,
                    VideoUrl = link,
                    Title = ServerName,
                },
            ];
        }

        var document = HtmlHelper.Parse(response);

        var mediaUrl = document.DocumentNode.SelectSingleNode(".//iframe").Attributes["src"].Value;
        if (string.IsNullOrWhiteSpace(mediaUrl))
            return [];

        if (mediaUrl.Contains("filemoon"))
            return await new FilemoonExtractor(HttpClientFactory).ExtractAsync(
                mediaUrl,
                cancellationToken
            );

        return await new GogoCdnExtractor(HttpClientFactory).ExtractAsync(
            mediaUrl,
            cancellationToken
        );
    }
}
