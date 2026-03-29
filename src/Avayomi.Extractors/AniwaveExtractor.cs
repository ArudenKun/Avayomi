using System.Text.Json.Nodes;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

/// <summary>
/// Extractor for Aniwave.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="AniwaveExtractor"/>.
/// </remarks>
public class AniwaveExtractor : VideoExtractorBase
{
    /// <summary>
    /// Name of the video server for Aniwave. It can either be "Mcloud" or "Vizcloud"
    /// </summary>
    public override string ServerName { get; }

    /// <summary>
    /// Extractor for Aniwave.
    /// </summary>
    /// <remarks>
    /// Initializes an instance of <see cref="AniwaveExtractor"/>.
    /// </remarks>
    public AniwaveExtractor(IHttpClientFactory httpClientFactory, string serverName)
        : base(httpClientFactory)
    {
        ServerName = serverName;
    }

    /// <inheritdoc />
    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateProviderHttpClient();

        var list = new List<VideoSource>();

        var isMcloud = ServerName.Equals("MyCloud", StringComparison.OrdinalIgnoreCase);
        var server = isMcloud ? "Mcloud" : "Vizcloud";
        var vidId = new Stack<string>(url.Split('/')).Pop().Split('?').FirstOrDefault();
        var url2 = $"https://9anime.eltik.net/raw{server}?query={vidId}&apikey=chayce";

        var response = await http.ExecuteAsync(url2, cancellationToken);
        var apiUrl = JsonNode.Parse(response)?["rawURL"]?.ToString();
        if (string.IsNullOrWhiteSpace(apiUrl))
            return list;

        var referer = isMcloud ? "https://mcloud.to/" : "https://9anime.to/";

        response = await http.ExecuteAsync(
            apiUrl!,
            new Dictionary<string, string> { ["Referer"] = referer },
            cancellationToken
        );

        var data = JsonNode.Parse(response)!["data"]!;

        var file = data["media"]!["sources"]![0]!["file"]!.ToString();

        list.Add(
            new VideoSource
            {
                VideoUrl = file,
                Headers = new Dictionary<string, string> { ["Referer"] = referer },
                Format = VideoType.M3U8,
                Resolution = "Multi Quality",
            }
        );

        return list;
    }
}
