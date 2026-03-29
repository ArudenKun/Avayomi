using System.Text;
using System.Text.Json.Nodes;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public class StreamSbExtractor : VideoExtractorBase
{
    private readonly char[] _hexArray = "0123456789ABCDEF".ToCharArray();

    public StreamSbExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override string ServerName => "StreamSB";

    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateClient();

        var id = url.FindBetween("/e/", ".html");
        if (string.IsNullOrWhiteSpace(id))
            id = url.Split(["/e/"], StringSplitOptions.None)[1];

        var bytes = Encoding.ASCII.GetBytes($"||{id}||||streamsb");
        var bytesToHex = BytesToHex(bytes);

        var source = await http.ExecuteAsync(
            "https://raw.githubusercontent.com/jerry08/anistream-extras/main/streamsb.txt",
            cancellationToken
        );

        var jsonLink = $"{source.Trim()}/{bytesToHex}/";

        headers = new Dictionary<string, string>
        {
            { "watchsb", "sbstream" },
            { "User-Agent", HttpHelper.ChromeUserAgent() },
            { "Referer", url },
        };

        var response = await http.ExecuteAsync(jsonLink, headers, cancellationToken);

        var data = JsonNode.Parse(response)!;
        var masterUrl = data["stream_data"]?["file"]?.ToString().Trim('"')!;

        return
        [
            new VideoSource
            {
                Format = VideoType.M3U8,
                VideoUrl = masterUrl,
                Headers = headers,
                Resolution = "Multi Quality",
            },
        ];
    }

    private string BytesToHex(byte[] bytes)
    {
        var hexChars = new char[bytes.Length * 2];
        for (var j = 0; j < bytes.Length; j++)
        {
            var v = bytes[j] & 0xFF;

            hexChars[j * 2] = _hexArray[v >> 4];
            hexChars[(j * 2) + 1] = _hexArray[v & 0x0F];
        }

        return new string(hexChars);
    }
}
