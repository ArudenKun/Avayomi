using System.Text.RegularExpressions;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public partial class KwikExtractor : VideoExtractorBase
{
    private const string Host = "https://animepahe.com";

    private const string Map = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/";

    public KwikExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    [GeneratedRegex(@"https://kwik\..+?/.*?/[A-Za-z0-9]+")]
    private static partial Regex RedirectRegex { get; }

    [GeneratedRegex("""\(\"(\w+)\",\d+,\"(\w+)\",(\d+),(\d+),(\d+)\)""")]
    private static partial Regex ParamRegex { get; }

    [GeneratedRegex("action=\"(.+?)\"")]
    private static partial Regex UrlRegex { get; }

    [GeneratedRegex("value=\"(.+?)\"")]
    private static partial Regex TokenRegex { get; }

    public override string ServerName => "Kwik";

    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateProviderHttpClient();

        var response = await http.ExecuteAsync(
            url,
            new Dictionary<string, string> { { "Referer", Host } },
            cancellationToken
        );

        //var kwikLink = _redirectRegex.Match(response).Groups[1].Value;
        var kwikLink = RedirectRegex.Match(response).Value;

        var kwikRes = await http.GetAsync(kwikLink, cancellationToken);
        var text = await kwikRes.Content.ReadAsStringAsync(cancellationToken);
        var cookies = kwikRes.Headers.GetValues("set-cookie").ElementAt(0);
        var groups = ParamRegex.Match(text).Groups.OfType<Group>().ToArray();
        var fullKey = groups[1].Value;
        var key = groups[2].Value;
        var v1 = groups[3].Value;
        var v2 = groups[4].Value;

        var decrypted = Decrypt(fullKey, key, int.Parse(v1), int.Parse(v2));
        var postUrl = UrlRegex.Match(decrypted).Groups.OfType<Group>().ToArray()[1].Value;
        var token = TokenRegex.Match(decrypted).Groups.OfType<Group>().ToArray()[1].Value;

        headers = new Dictionary<string, string>()
        {
            { "Referer", kwikLink },
            { "Cookie", cookies },
        };

        var formContent = new FormUrlEncodedContent(
            new KeyValuePair<string?, string?>[] { new("_token", token) }
        );

        var request = new HttpRequestMessage(HttpMethod.Post, postUrl);
        for (var j = 0; j < headers.Count; j++)
            request.Headers.TryAddWithoutValidation(
                headers.ElementAt(j).Key,
                headers.ElementAt(j).Value
            );

        if (!request.Headers.Contains("User-Agent"))
        {
            request.Headers.Add("User-Agent", HttpHelper.ChromeUserAgent());
        }

        request.Content = formContent;

        http = HttpClientFactory.CreateProviderHttpClient();

        //var allowAutoRedirect = http.GetAllowAutoRedirect();

        http.SetAllowAutoRedirect(false);

        var response2 = await http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        var mp4Url = response2.Headers.Location!.ToString();

        return
        [
            new()
            {
                VideoUrl = mp4Url,
                Format = VideoType.Container,
                FileType = "mp4",
            },
        ];
    }

    private int GetString(string content, int s1)
    {
        var s2 = 10;
        var slice = Map.Substring(0, s2);
        double acc = 0;

        var reversedMap = content.Reverse().ToArray();

        for (var i = 0; i < reversedMap.Length; i++)
        {
            var c = reversedMap[i];
            acc += (char.IsDigit(c) ? int.Parse(c.ToString()) : 0) * Math.Pow(s1, i);
        }

        var k = "";

        while (acc > 0)
        {
            k = slice[(int)(acc % s2)] + k;
            acc = (acc - (acc % s2)) / s2;
        }

        return int.TryParse(k, out var l) ? l : 0;
    }

    private string Decrypt(string fullKey, string key, int v1, int v2)
    {
        var r = "";
        for (var i = 0; i < fullKey.Length; i++)
        {
            var s = "";
            while (fullKey[i] != key[v2])
            {
                s += fullKey[i];
                i++;
            }

            for (var j = 0; j < key.Length; j++)
            {
                s = s.Replace(key[j].ToString(), j.ToString());
            }

            r += (char)(GetString(s, v2) - v1);
        }

        return r;
    }
}
