using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public class MegaCloudExtractor : VideoExtractorBase
{
    private const string SourcesUrl = "/embed-2/v3/e-1/getSources?id=";
    private const string SourcesSplitter = "/e-1/";
    private string? _cachedKey;

    public MegaCloudExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override string ServerName => "MegaCloud";

    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var uri = new Uri(url);
        var serverUrl = $"{uri.Scheme}://{uri.Host}";

        // 1. Extract ID
        var id = url.Split([SourcesSplitter], StringSplitOptions.None)
            .LastOrDefault()
            ?.Split('?')
            .FirstOrDefault();
        if (string.IsNullOrEmpty(id))
            throw new Exception("Failed to extract ID");

        var http = HttpClientFactory.CreateProviderHttpClient();

        headers = new Dictionary<string, string>(headers) { ["Referer"] = serverUrl };

        // 2. Get the Nonce from the embed page
        var embedPage = await http.ExecuteAsync(url, headers, cancellationToken);
        var nonce = ExtractNonce(embedPage);

        // 3. Fetch Sources
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{serverUrl}{SourcesUrl}{id}&_k={nonce}"
        );
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Add("Referer", serverUrl);

        var response = await http.SendAsync(request, cancellationToken);
        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonNode.Parse(jsonResponse);

        if (data?["sources"] is null)
            return [];

        // 4. Decrypt Sources
        var isEncrypted = data["encrypted"]?.GetValue<bool>() ?? true;
        var key = _cachedKey ?? await RequestNewKeyAsync();

        // 5. Extract Subtitles from tracks (only captions)
        var subtitles = new List<Subtitle>();
        if (data["tracks"] is JsonArray tracks)
        {
            foreach (var track in tracks)
            {
                var kind = track?["kind"]?.ToString();

                // Only include captions
                if (!string.Equals(kind, "captions", StringComparison.OrdinalIgnoreCase))
                    continue;

                var file = track?["file"]?.ToString();
                var label = track?["label"]?.ToString();

                if (string.IsNullOrWhiteSpace(file))
                    continue;

                var subtitleType = GetSubtitleType(file);
                var language = !string.IsNullOrEmpty(label) ? label : "Unknown";

                subtitles.Add(new Subtitle(file, language, subtitleType));
            }
        }

        var videoSources = new List<VideoSource>();
        foreach (var source in data["sources"]!.AsArray())
        {
            var file = source?["file"]?.ToString();
            if (string.IsNullOrEmpty(file))
                continue;

            var m3U8Url = isEncrypted && !file.Contains(".m3u8") ? DecryptLocal(file, key) : file;

            videoSources.Add(
                new VideoSource
                {
                    VideoUrl = m3U8Url,
                    Format = VideoType.M3U8,
                    Resolution = "Multi Quality",
                    Headers = headers,
                    Subtitles = subtitles,
                }
            );
        }

        return videoSources;
    }

    private static SubtitleType GetSubtitleType(string url)
    {
        if (url.Contains(".vtt", StringComparison.OrdinalIgnoreCase))
            return SubtitleType.Vtt;
        if (url.Contains(".ass", StringComparison.OrdinalIgnoreCase))
            return SubtitleType.Ass;
        if (url.Contains(".srt", StringComparison.OrdinalIgnoreCase))
            return SubtitleType.Srt;
        return SubtitleType.Vtt; // Default to VTT
    }

    private static string ExtractNonce(string html)
    {
        // Try 48-char match first
        var match1 = Regex.Match(html, @"\b[a-zA-Z0-9]{48}\b");
        if (match1.Success)
            return match1.Value;

        // Try triplet match
        var match2 = Regex.Match(
            html,
            @"\b([a-zA-Z0-9]{16})\b.*?\b([a-zA-Z0-9]{16})\b.*?\b([a-zA-Z0-9]{16})\b"
        );
        if (match2.Success)
            return match2.Groups[1].Value + match2.Groups[2].Value + match2.Groups[3].Value;

        throw new Exception("Nonce not found");
    }

    private static string DecryptLocal(string cipherText, string secret)
    {
        // The "password" for the OpenSSL derivation is the secret key
        return DecryptOpenSsl(cipherText, secret);
    }

    private async Task<string> RequestNewKeyAsync()
    {
        var json = await HttpClientFactory
            .CreateClient()
            .GetStringAsync(
                "https://raw.githubusercontent.com/yogesh-hacker/MegacloudKeys/refs/heads/main/keys.json"
            );

        var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);

        if (
            data is null
            || !data.TryGetValue("cinesrc", out var cinesrc)
            || !cinesrc.TryGetValue("getStream", out var key)
        )
        {
            throw new Exception("Key not found");
        }

        _cachedKey = key;
        return _cachedKey;
    }

    // --- Standard OpenSSL Decryption Helper ---
    private static string DecryptOpenSsl(string base64Input, string password)
    {
        var data = Convert.FromBase64String(base64Input);
        var salt = data.Skip(8).Take(8).ToArray();
        var ciphertext = data.Skip(16).ToArray();

        var (key, iv) = DeriveKeyAndIv(password, salt);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(ciphertext);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);
        return reader.ReadToEnd();
    }

    private static (byte[] Key, byte[] Iv) DeriveKeyAndIv(string password, byte[] salt)
    {
        var passBytes = Encoding.UTF8.GetBytes(password);
        var combined = new List<byte>();
        var currentHash = Array.Empty<byte>();

        while (combined.Count < 48) // 32 for Key + 16 for IV
        {
            using var md5 = MD5.Create();
            var input = currentHash.Concat(passBytes).Concat(salt).ToArray();
            currentHash = md5.ComputeHash(input);
            combined.AddRange(currentHash);
        }

        return (combined.Take(32).ToArray(), combined.Skip(32).Take(16).ToArray());
    }
}
