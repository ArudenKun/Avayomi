using Avayomi.Core.Extensions;
using Avayomi.Core.Providers;
using Avayomi.Core.Videos;
using Avayomi.Extractors;

namespace Avayomi.Providers.Movie;

public class MovieBaseProvider : IVideoExtractorProvider
{
    public MovieBaseProvider(IHttpClientFactory httpClientFactory)
    {
        HttpClientFactory = httpClientFactory;
    }

    protected IHttpClientFactory HttpClientFactory { get; }

    protected HttpClient HttpClient => HttpClientFactory.CreateProviderHttpClient();

    public virtual IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        var domain = new Uri(server.Embed.Url).Host;
        if (domain.StartsWith("www."))
            domain = domain.Substring(4);

        return domain.ToLower() switch
        {
            "filemoon.to" or "filemoon.sx" => new FilemoonExtractor(HttpClientFactory),
            "rapid-cloud.co" => new RapidCloudExtractor(HttpClientFactory),
            "streamtape.com" => new StreamTapeExtractor(HttpClientFactory),
            "vidstream.pro" => new VidStreamExtractor(HttpClientFactory),
            "mp4upload.com" => new Mp4UploadExtractor(HttpClientFactory),
            "playtaku.net" or "goone.pro" => new GogoCdnExtractor(HttpClientFactory),
            "alions.pro" => new ALionsExtractor(HttpClientFactory),
            "awish.pro" => new AWishExtractor(HttpClientFactory),
            "dood.wf" => new DoodExtractor(HttpClientFactory),
            "ok.ru" => new OkRuExtractor(HttpClientFactory),
            // "streamlare.com" => null,
            _ => null,
        };
    }

    /// <inheritdoc />
    public virtual async ValueTask<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default
    )
    {
        if (!Uri.IsWellFormedUriString(server.Embed.Url, UriKind.Absolute))
            return [];

        var extractor = GetVideoExtractor(server);
        if (extractor is null)
            return [];

        var videos = await extractor.ExtractAsync(server.Embed.Url, cancellationToken);

        videos.ForEach(x => x.VideoServer = server);

        return videos;
    }
}
