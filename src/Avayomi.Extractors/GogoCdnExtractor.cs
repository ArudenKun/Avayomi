using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public class GogoCdnExtractor : VideoExtractorBase
{
    public GogoCdnExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override string ServerName => "Gogo";

    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateProviderHttpClient();

        var host = new Uri(url).Host;

        var list = new List<VideoSource>();

        var response = await http.ExecuteAsync(url, cancellationToken);

        var keys = KeysAndIv();

        var document = HtmlHelper.Parse(response);

        var dataValue = document
            .DocumentNode.Descendants()
            .Where(x => x.Name == "script")
            .FirstOrDefault(x => x.Attributes["data-name"].Value == "episode")
            ?.Attributes["data-value"]
            .Value;
        if (string.IsNullOrWhiteSpace(dataValue))
            return list;

        var decrypted = CryptoHandler(dataValue, keys.Item1, keys.Item3, false).Replace("\t", "");
        var id = decrypted.FindBetween("", "&");
        var end = decrypted.SubstringAfter(id);

        var link =
            $"https://{host}/encrypt-ajax.php?id={CryptoHandler(id, keys.Item1, keys.Item3)}{end}&alias={id}";

        var encHtmlData = await http.ExecuteAsync(
            link,
            new Dictionary<string, string>()
            {
                { "X-Requested-With", "XMLHttpRequest" },
                //{ "Referer", host },
            },
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(encHtmlData))
            return list;

        var jsonObj = JsonNode.Parse(encHtmlData)!;
        var jumbledJson = CryptoHandler(jsonObj["data"]!.ToString(), keys.Item2, keys.Item3, false);
        jumbledJson = jumbledJson.Replace(@"o""<P{#meme"":""", @"e"":[{""file"":""");

        var source = JsonNode.Parse(jumbledJson)!["source"]!.ToString();
        var array = JsonNode.Parse(source)!.AsArray();

        var sourceBk = JsonNode.Parse(jumbledJson)!["source_bk"]!.ToString();
        var arrayBk = JsonNode.Parse(sourceBk)!.AsArray();

        void AddSources(JsonArray jsonArray, bool backup)
        {
            for (var i = 0; i < jsonArray.Count; i++)
            {
                var label = jsonArray[i]!["label"]!.ToString();
                var fileUrl = jsonArray[i]!["file"]!.ToString().Trim('"');
                var type = jsonArray[i]?["type"]?.ToString().ToLower();

                if (type == "hls" || type == "auto")
                {
                    list.Add(
                        new VideoSource
                        {
                            Format = VideoType.M3U8,
                            VideoUrl = fileUrl,
                            Resolution = "Multi Quality" + (backup ? " (Backup)" : ""),
                            Headers = new Dictionary<string, string> { ["Referer"] = url },
                        }
                    );
                }
                else
                {
                    list.Add(
                        new VideoSource
                        {
                            Format = VideoType.Container,
                            VideoUrl = fileUrl,
                            Resolution = label,
                            Headers = new Dictionary<string, string> { ["Referer"] = url },
                        }
                    );
                }
            }
        }

        AddSources(array, false);
        AddSources(arrayBk, true);

        return list;
    }

    private static Tuple<string, string, string> KeysAndIv() =>
        new(
            "37911490979715163134003223491201",
            "54674138327930866480207815084989",
            "3134003223491201"
        );

    private static string CryptoHandler(
        string dataValue,
        string key,
        string iv,
        bool encrypt = true
    )
    {
        //var key = Encoding.UTF8.GetBytes("63976882873559819639988080820907");
        //var iv = Encoding.UTF8.GetBytes("4770478969418267");

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var ivBytes = Encoding.UTF8.GetBytes(iv);

        var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        if (encrypt)
        {
            // Convert from Base64 to binary
            var bytIn = Encoding.ASCII.GetBytes(dataValue);

            //var padding = new byte[] { 0x8, 0xe, 0x3, 0x8, 0x9, 0x3, 0x4, 0x9 };
            //bytIn = bytIn.Concat(padding).ToArray();

            // Create a MemoryStream
            var ms = new MemoryStream();

            // Create Crypto Stream that encrypts a stream
            var cs = new CryptoStream(
                ms,
                aes.CreateEncryptor(keyBytes, ivBytes),
                CryptoStreamMode.Write
            );

            // Write content into MemoryStream
            cs.Write(bytIn, 0, bytIn.Length);
            cs.FlushFinalBlock();

            var bytOut = ms.ToArray();
            return Convert.ToBase64String(bytOut);
        }
        else
        {
            // Convert from Base64 to binary
            var bytIn = Convert.FromBase64String(dataValue);

            // Create a MemoryStream
            var ms = new MemoryStream(bytIn, 0, bytIn.Length);

            // Create a CryptoStream that decrypts the data
            var cs = new CryptoStream(
                ms,
                aes.CreateDecryptor(keyBytes, ivBytes),
                CryptoStreamMode.Read
            );

            // Read the Crypto Stream
            var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
    }
}
