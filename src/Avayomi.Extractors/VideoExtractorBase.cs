using Avayomi.Core.Videos;

namespace Avayomi.Extractors;

public abstract class VideoExtractorBase : IVideoExtractor
{
    public abstract string ServerName { get; }

    protected IHttpClientFactory HttpClientFactory { get; }

    public VideoExtractorBase(IHttpClientFactory httpClientFactory)
    {
        HttpClientFactory = httpClientFactory;
    }

    public virtual ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default
    ) => ExtractAsync(url, [], cancellationToken);

    public abstract ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    );
}
