using System.Text.Json.Nodes;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;
using Avayomi.Extractors.Decryptors;

namespace Avayomi.Extractors;

public class VidCloudExtractor : VideoExtractorBase
{
    private const string Host = "https://dokicloud.one";
    private const string Host2 = "https://rabbitstream.net";
    private readonly bool _isAlternative;

    public VidCloudExtractor(IHttpClientFactory httpClientFactory, bool isAlternative)
        : base(httpClientFactory)
    {
        _isAlternative = isAlternative;
    }

    public override string ServerName => "VidCloud";

    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateClient();

        var id = new Stack<string>(url.Split('/')).Pop().Split('?')[0];

        headers = new Dictionary<string, string>
        {
            { "X-Requested-With", "XMLHttpRequest" },
            { "Referer", url },
            { "User-Agent", HttpHelper.ChromeUserAgent() },
        };

        var response = await http.ExecuteAsync(
            $"{(_isAlternative ? Host2 : Host)}/ajax/embed-4/getSources?id={id}",
            headers,
            cancellationToken
        );

        var data = JsonNode.Parse(response)!;
        var sourcesJson = data["sources"]!.ToString();

        if (!IsValidJson(sourcesJson))
        {
            //var key = await _http.ExecuteAsync("https://raw.githubusercontent.com/consumet/rapidclown/rabbitstream/key.txt", cancellationToken);
            var key = await http.ExecuteAsync(
                "https://raw.githubusercontent.com/enimax-anime/key/e4/key.txt",
                cancellationToken
            );

            sourcesJson = VidCloudDecryptor.Decrypt(sourcesJson, key);
        }

        var subtitles = data["tracks"]!
            .AsArray()
            .Where(x => x!["kind"]?.ToString() == "captions")
            .Select(track => new Subtitle()
            {
                Url = track!["file"]!.ToString(),
                Language = track["label"]!.ToString(),
            })
            .ToList();

        var sources = JsonNode.Parse(sourcesJson)!.AsArray();

        var list = sources
            .Select(source => new VideoSource()
            {
                VideoUrl = source!["file"]!.ToString(),
                Format = source["file"]!.ToString().Contains(".m3u8")
                    ? VideoType.M3U8
                    : source["type"]!.ToString().ToLower() switch
                    {
                        "hls" => VideoType.Hls,
                        _ => VideoType.Container,
                    },
                Subtitles = subtitles,
            })
            .ToList();

        return list;
    }

    private static bool IsValidJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        value = value.Trim();
        if (
            (value.StartsWith("{") && value.EndsWith("}"))
            || (value.StartsWith("[") && value.EndsWith("]"))
        )
        {
            try
            {
                var obj = JsonNode.Parse(value);
                return obj is not null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        return false;
    }
}
