using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public class YourUploadExtractor : VideoExtractorBase
{
    public YourUploadExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override string ServerName => "YourUpload";

    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    ) => await ExtractAsync(url, null, cancellationToken);

    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        string? quality = null,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateClient();

        var headers = new Dictionary<string, string>()
        {
            ["Referer"] = "https://www.yourupload.com/",
        };

        var response = await http.ExecuteAsync(url, headers, cancellationToken);

        var document = HtmlHelper.Parse(response);

        var baseData = document
            .DocumentNode.Descendants()
            .FirstOrDefault(x => x.Name == "script" && x.InnerText.Contains("jwplayerOptions"))
            ?.InnerText;

        if (string.IsNullOrEmpty(baseData))
            return [];

        var basicUrl = baseData.RemoveWhitespaces().SubstringAfter("file:'").SubstringBefore("',");

        return
        [
            new()
            {
                Format = VideoType.Container,
                VideoUrl = basicUrl,
                Resolution = quality,
                Headers = headers,
                Title = $"{quality} - {ServerName}",
            },
        ];
    }
}
