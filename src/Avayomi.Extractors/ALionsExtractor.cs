using System.Text.RegularExpressions;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public partial class ALionsExtractor : VideoExtractorBase
{
    public ALionsExtractor(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    /// <inheritdoc />
    public override string ServerName => "ALions / Vidhide";

    /// <inheritdoc />
    public override async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateProviderHttpClient();

        var response = await http.ExecuteAsync(url, cancellationToken);

        var script =
            ScriptRegex1().Match(response).Groups.OfType<Group>().ElementAtOrDefault(1)?.Value
            ?? ScriptRegex2().Match(response).Groups.OfType<Group>().ElementAtOrDefault(1)?.Value;

        if (string.IsNullOrEmpty(script))
            return [];

        var unpackedScript = JavaScriptUnpacker.UnpackAndCombine(script);

        var mediaUrl = MediaUrlRegex()
            .Match(unpackedScript)
            .Groups.OfType<Group>()
            .ElementAtOrDefault(1)
            ?.Value;

        if (string.IsNullOrEmpty(mediaUrl))
            return [];

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

    [GeneratedRegex("<script type=\"text/javascript\">(eval.+)\n</script>")]
    private static partial Regex ScriptRegex1();

    [GeneratedRegex("<script type=\'text/javascript\'>(eval.+)\n</script>")]
    private static partial Regex ScriptRegex2();

    [GeneratedRegex("file:\"([^\"]+)\"\\}")]
    private static partial Regex MediaUrlRegex();
}
