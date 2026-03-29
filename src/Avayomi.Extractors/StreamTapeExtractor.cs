using System.Text.RegularExpressions;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public partial class StreamTapeExtractor : VideoExtractorBase
{
    public StreamTapeExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override string ServerName => "StreamTape";

    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateClient();

        var response = await http.ExecuteAsync(
            url.Replace("tape.com", "adblocker.xyz"),
            cancellationToken
        );

        var reg = LinkRegex.Match(response);

        var vidUrl = $"https:{reg.Groups[1].Value + reg.Groups[2].Value.Substring(3)}";

        return
        [
            new VideoSource
            {
                Format = VideoType.M3U8,
                VideoUrl = vidUrl,
                Resolution = "Multi Quality",
            },
        ];
    }

    [GeneratedRegex(@"'robotlink'\)\.innerHTML = '(.+?)'\+ \('(.+?)'\)")]
    private static partial Regex LinkRegex { get; }
}
