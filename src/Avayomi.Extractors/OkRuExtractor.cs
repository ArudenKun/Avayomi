using System.Text.RegularExpressions;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public partial class OkRuExtractor : VideoExtractorBase
{
    public OkRuExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override string ServerName => "OkRu";

    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateClient();

        var response = await http.ExecuteAsync(url, cancellationToken);

        var mediaUrl = MediaUrlRegex().Match(response);

        return
        [
            new VideoSource
            {
                Format = VideoType.M3U8,
                VideoUrl = mediaUrl.Value,
                Title = ServerName,
            },
            new VideoSource
            {
                Format = VideoType.Dash,
                VideoUrl = mediaUrl.NextMatch().Value,
                Title = ServerName,
            },
        ];
    }

    [GeneratedRegex(@"https://vd\d+\.mycdn\.me/e[^\\]+")]
    private static partial Regex MediaUrlRegex();
}
