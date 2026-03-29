using System.Text.RegularExpressions;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

/// <summary>
/// Extractor for AWish.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="AWishExtractor"/>.
/// </remarks>
public partial class AWishExtractor : VideoExtractorBase
{
    public AWishExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    /// <inheritdoc />
    public override string ServerName => "AWish";

    /// <inheritdoc />
    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateProviderHttpClient();

        var response = await http.ExecuteAsync(url, cancellationToken);

        var document = HtmlHelper.Parse(response);

        var script = document
            .DocumentNode.Descendants()
            .FirstOrDefault(x => x.Name == "script" && x.InnerText.Contains("m3u8"))
            ?.InnerText;

        // Sometimes the script body is packed, sometimes it isn't
        var scriptBody = JavaScriptUnpacker.IsPacked(script)
            ? JavaScriptUnpacker.UnpackAndCombine(script)
            : script;

        if (string.IsNullOrEmpty(scriptBody))
            return [];

        //var mediaUrl = new Regex("file:\"([^\"]+)\"\\}").Match(scriptBody)
        var mediaUrl = MediaUrlRegex().Match(scriptBody).Groups.OfType<Group>().ToList()[1].Value;

        return
        [
            new VideoSource
            {
                Format = VideoType.M3U8,
                VideoUrl = mediaUrl,
                Title = ServerName,
            },
        ];
    }

    [GeneratedRegex("file:\"([^\"]+)\"")]
    private static partial Regex MediaUrlRegex();
}
