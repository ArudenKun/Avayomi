using System.Text.Json.Nodes;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

/// <summary>
/// Extractor for FPlayer.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="FPlayerExtractor"/>.
/// </remarks>
public class FPlayerExtractor : VideoExtractorBase
{
    public FPlayerExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    /// <inheritdoc />
    public override string ServerName => "FPlayer";

    /// <inheritdoc />
    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateProviderHttpClient();
        var apiLink = url.Replace("/v/", "/api/source/");

        var list = new List<VideoSource>();

        try
        {
            headers = new Dictionary<string, string> { { "Referer", url } };

            var json = await http.PostAsync(apiLink, headers, cancellationToken);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var data = JsonNode.Parse(JsonNode.Parse(json)!["data"]!.ToString())!.AsArray();
                list.AddRange(
                    data.Select(t => new VideoSource
                    {
                        VideoUrl = t!["file"]!.ToString(),
                        Resolution = t["label"]!.ToString(),
                        Format = VideoType.Container,
                        FileType = t["type"]!.ToString(),
                    })
                );

                return list;
            }
        }
        catch
        {
            // Ignore
        }

        return list;
    }
}
