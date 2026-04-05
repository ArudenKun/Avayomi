using Avayomi.Core.AniList;
using Avayomi.Core.Anime;
using Avayomi.Core.Videos;
using Avayomi.Extractors;
using Avayomi.Providers.Anime.Zoro;

namespace Avayomi.Providers.Anime;

/// <summary>
/// Client for interacting with Kaido.to.
/// </summary>
public class Kaido : ZoroTheme
{
    public Kaido(IHttpClientFactory httpClientFactory, IAniListClient aniListClient)
        : base(httpClientFactory, aniListClient) { }

    public override string Key => Name;
    public override string Name => "Kaido";
    public override string Language => "en";
    public override string BaseUrl => "https://kaido.to";

    protected override List<string> HosterNames => ["Vidstreaming", "VidCloud", "StreamTape"];

    public override IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        var serverNameLower = server.Name.ToLower();

        return serverNameLower switch
        {
            var s when s.Contains("vidcloud") || s.Contains("vidstreaming") =>
                new MegaCloudExtractor(HttpClientFactory),
            var s when s.Contains("streamtape") => new StreamTapeExtractor(HttpClientFactory),
            _ => base.GetVideoExtractor(server),
        };
    }
}
