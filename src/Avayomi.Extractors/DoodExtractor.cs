using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public class DoodExtractor : VideoExtractorBase
{
    private static readonly Random Random = new();

    public DoodExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override string ServerName => "Dood";

    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateProviderHttpClient();

        var list = new List<VideoSource>();

        try
        {
            var response = await http.ExecuteAsync(
                url,
                new Dictionary<string, string>() { ["User-Agent"] = "Juro" },
                cancellationToken
            );

            if (!response.Contains("'/pass_md5/"))
                return [];

            var doodTld = url.SubstringAfter("https://dood.").SubstringBefore("/");
            var md5 = response.SubstringAfter("'/pass_md5/").SubstringBefore("',");
            var token = md5.Split(["/"], StringSplitOptions.None).LastOrDefault();
            var randomString = RandomString();
            var expiry = DateTime.Now.CurrentTimeMillis();

            var videoUrlStart = await http.ExecuteAsync(
                $"https://dood.{doodTld}/pass_md5/{md5}",
                new Dictionary<string, string>() { ["Referer"] = url, ["User-Agent"] = "Juro" },
                cancellationToken
            );

            var videoUrl = $"{videoUrlStart}{randomString}?token={token}&expiry={expiry}";

            list.Add(
                new()
                {
                    Format = VideoType.Container,
                    VideoUrl = videoUrl,
                    Resolution = "Default Quality",
                    Headers = new()
                    {
                        ["User-Agent"] = "Juro",
                        ["Referer"] = $"https://dood.{doodTld}",
                    },
                }
            );
        }
        catch
        {
            // Ignore
        }

        return list;
    }

    private static string RandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(
            Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray()
        );
    }
}
