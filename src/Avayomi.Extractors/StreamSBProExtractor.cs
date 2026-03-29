using System.Text.Json.Nodes;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public class StreamSbProExtractor : VideoExtractorBase
{
    private const string Alphabet =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public StreamSbProExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override string ServerName => "StreamSB Pro";

    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateClient();

        var id = url.FindBetween("/e/", ".html");
        if (string.IsNullOrWhiteSpace(id))
            id = url.Split(new[] { "/e/" }, StringSplitOptions.None)[1];

        var source = await http.ExecuteAsync(
            "https://raw.githubusercontent.com/jerry08/juro-data/main/streamsb.txt",
            cancellationToken
        );

        var jsonLink = $"{source.Trim()}/{Encode(id)}";

        headers = new Dictionary<string, string>()
        {
            //{ "watchsb", "streamsb" },
            { "watchsb", "sbstream" },
            { "User-Agent", HttpHelper.ChromeUserAgent() },
            { "Referer", url },
        };

        var response = await http.ExecuteAsync(jsonLink, headers, cancellationToken);

        var data = JsonNode.Parse(response);
        var masterUrl = data?["stream_data"]?["file"]?.ToString().Trim('"')!;

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

    private string Encode(string id)
    {
        id = $"{MakeId(12)}||{id}||{MakeId(12)}||streamsb";

        var output = "";
        var arr = id.ToArray();

        for (var i = 0; i < arr.Length; i++)
        {
            output += Convert.ToString(Convert.ToInt32(((int)arr[i]).ToString(), 10), 16);
        }

        return output;
    }

    private string MakeId(int length)
    {
        var output = "";

        for (var i = 0; i < length; i++)
        {
            output += Alphabet[(int)Math.Floor(new Random().NextDouble() * Alphabet.Length)];
        }

        return output;
    }
}
