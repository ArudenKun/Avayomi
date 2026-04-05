using System.Text.Json.Nodes;
using AngleSharp.Dom;
using Avayomi.Core;
using Avayomi.Core.AniList;
using Avayomi.Core.Anime;
using Avayomi.Core.Extensions;
using Avayomi.Core.Providers.Anime;
using Avayomi.Core.Tasks;
using Avayomi.Core.Videos;
using Avayomi.Extractors;

namespace Avayomi.Providers.Anime;

/// <summary>
/// Client for interacting with 9anime.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="NineAnime"/>.
/// </remarks>
public class NineAnime : AnimeBaseProvider, IAnimeProvider, IPopularProvider, ILastUpdatedProvider
{
    /// <summary>
    /// Client for interacting with 9anime.
    /// </summary>
    /// <remarks>
    /// Initializes an instance of <see cref="NineAnime"/>.
    /// </remarks>
    public NineAnime(IHttpClientFactory httpClientFactory, IAniListClient aniListClient)
        : base(httpClientFactory, aniListClient) { }

    public string Key => Name;

    public string Name => "NineAnime";

    public string Language => "en";

    public bool IsDubAvailableSeparately => false;

    public string BaseUrl => "https://9anime.pe";

    /// <inheritdoc />
    public async ValueTask<List<AnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var response = await HttpClient.ExecuteAsync(
            $"{BaseUrl}/filter?keyword={Uri.EscapeDataString(query).Replace("%20", "+")}",
            new Dictionary<string, string> { ["Referer"] = BaseUrl },
            cancellationToken
        );

        return ParseAnimeSearchResponse(response);
    }

    /// <inheritdoc cref="SearchAsync"/>
    public async ValueTask<List<AnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await HttpClient.ExecuteAsync(
            $"{BaseUrl}/filter?sort=trending&page={page}",
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <summary>
    /// Gets anime in new season.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="cancellationToken"></param>
    public async ValueTask<List<AnimeInfo>> GetLastUpdatedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateProviderHttpClient();
        var response = await http.ExecuteAsync(
            $"{BaseUrl}/filter?sort=recently_updated&page={page}",
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    private List<AnimeInfo> ParseAnimeResponse(string? response)
    {
        var list = new List<AnimeInfo>();

        if (string.IsNullOrWhiteSpace(response))
            return list;

        var document = HtmlHelper.Parse(response);

        // Target items inside the list-items container directly
        var nodes = document.QuerySelectorAll("#list-items > div.item");

        foreach (var node in nodes)
        {
            // 1. Get the anchor for ID and link info
            var aniAnchor = node.QuerySelector("div.ani a");
            var href = aniAnchor?.GetAttribute("href") ?? "";

            // 2. Get the title anchor
            var titleAnchor = node.QuerySelector("div.info div.b1 a");

            var animeInfo = new AnimeInfo
            {
                Id = href, // You can still .Split('/') here if needed
                Title = titleAnchor?.TextContent.Trim() ?? "",
                Image = node.QuerySelector("div.ani a img")?.GetAttribute("src") ?? "",
            };

            list.Add(animeInfo);
        }

        return list;
    }

    private List<AnimeInfo> ParseAnimeSearchResponse(string? response)
    {
        var list = new List<AnimeInfo>();
        if (string.IsNullOrWhiteSpace(response))
            return list;

        // 1. Extract HTML from the JSON result
        var data = JsonNode.Parse(response);
        var html = data?["result"]?["html"]?.ToString();
        if (string.IsNullOrWhiteSpace(html))
            return list;

        var document = HtmlHelper.Parse(html);

        // 2. Target the search result anchors
        var nodes = document.QuerySelectorAll("a.item");

        foreach (var node in nodes)
        {
            var animeInfo = new AnimeInfo
            {
                Id = node.GetAttribute("href") ?? "",
                Title = node.QuerySelector("div.name.d-title")?.TextContent.Trim() ?? "",
                Image = node.QuerySelector("img")?.GetAttribute("src") ?? "",

                // XPath: span[last()] -> CSS: span:last-child
                Released = node.QuerySelector("div.meta span:last-child")?.TextContent.Trim() ?? "",

                // XPath: span[last()-1] -> CSS: span:nth-last-child(2)
                Type =
                    node.QuerySelector("div.meta span:nth-last-child(2)")?.TextContent.Trim() ?? "",
            };

            list.Add(animeInfo);
        }

        return list;
    }

    /// <inheritdoc />
    public async ValueTask<AnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await HttpClient.ExecuteAsync($"{BaseUrl}{id}", cancellationToken);
        var document = HtmlHelper.Parse(response);

        // 1. Target the main title node once to reuse it
        var titleNode = document.QuerySelector("h1.title");

        var anime = new AnimeInfo
        {
            Id = id,
            Title = titleNode?.TextContent.Trim() ?? string.Empty,
            OtherNames = titleNode?.GetAttribute("data-jp"),

            // CSS Breadcrumbs: div.binfo div.poster span img
            Image =
                document.QuerySelector("div.binfo div.poster span img")?.GetAttribute("src")
                ?? string.Empty,

            Summary = document.QuerySelector("div.content")?.TextContent.Trim() ?? string.Empty,
        };

        // 2. Target the metadata container
        // XPath: .//div[contains(@class, 'meta')][1]/div
        var metaItems = document.QuerySelectorAll("div.meta:first-of-type > div");

        if (metaItems.Any())
        {
            // Helper to find a value based on a label inside the meta div
            string? GetMetaValue(string label) =>
                metaItems
                    .FirstOrDefault(x =>
                        x.TextContent.Contains(label, StringComparison.OrdinalIgnoreCase)
                    )
                    ?.QuerySelector("span")
                    ?.TextContent.Trim();

            anime.Released = GetMetaValue("aired");
            anime.Type = GetMetaValue("type");
            anime.Status = GetMetaValue("status");

            // Specialized logic for Genres (mapping multiple anchors)
            var genresNode = metaItems.FirstOrDefault(x =>
                x.TextContent.Contains("genres", StringComparison.OrdinalIgnoreCase)
            );

            if (genresNode is not null)
            {
                var genres = genresNode
                    .QuerySelectorAll("span a")
                    .Select(x => new Genre(x.TextContent.Trim()));
                anime.Genres.AddRange(genres);
            }
        }

        return anime;
    }

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var list = new List<Episode>();

        // 1. Fetch the main anime page to get the data-id
        var response = await HttpClient.ExecuteAsync($"{BaseUrl}{id}", cancellationToken);
        var document = HtmlHelper.Parse(response);

        // CSS Selector: Any div with a data-id attribute
        var dataId = document.QuerySelector("div[data-id]")?.GetAttribute("data-id");

        if (string.IsNullOrWhiteSpace(dataId))
            return list;

        // 2. Generate the VRF and fetch the AJAX episode list
        var vrf = await EncodeVrfAsync(dataId, cancellationToken);
        var response2 = await HttpClient.ExecuteAsync(
            $"{BaseUrl}/ajax/episode/list/{dataId}?vrf={vrf}",
            new Dictionary<string, string> { ["url"] = BaseUrl + id },
            cancellationToken
        );

        // 3. Parse the JSON result to get the HTML fragment
        var jsonResult = JsonNode.Parse(response2)?["result"]?.ToString();
        if (string.IsNullOrWhiteSpace(jsonResult))
            return list;

        var epDocument = HtmlHelper.Parse(jsonResult);

        // 4. Extract episode nodes using CSS breadcrumbs
        // HAP: .//div[contains(@class, 'episodes')]/ul/li/a
        var nodes = epDocument.QuerySelectorAll("div.episodes ul li a");

        foreach (var node in nodes)
        {
            // Split data-ids attribute (e.g., "id1,id2")
            var possibleIds = node.GetAttribute("data-ids")?.Split(',');
            if (possibleIds == null || possibleIds.Length == 0)
                continue;

            var numStr = node.GetAttribute("data-num") ?? "0";

            list.Add(
                new Episode
                {
                    Id = possibleIds[0], // Sub is typically the first ID
                    Name = node.QuerySelector("span")?.TextContent?.Trim(),
                    Number = int.TryParse(numStr, out var n) ? (int)n : 0,
                }
            );
        }

        return list;
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        var list = new List<VideoServer>();

        // 1. Generate VRF and fetch the AJAX server list
        var vrf = await EncodeVrfAsync(episodeId, cancellationToken);
        var response = await HttpClient.ExecuteAsync(
            $"{BaseUrl}/ajax/server/list/{episodeId}?vrf={vrf}",
            cancellationToken
        );

        // 2. Extract HTML fragment from JSON
        var jsonNode = JsonNode.Parse(response);
        var html = jsonNode?["result"]?.ToString();
        if (string.IsNullOrWhiteSpace(html))
            return list;

        var document = HtmlHelper.Parse(html);

        // 3. Select server list items using CSS Breadcrumbs
        // HAP: .//div[@class='type']/ul/li
        var nodes = document.QuerySelectorAll("div.type ul li").ToList();

        if (nodes.Count == 0)
            return list;

        // 4. Parallel execution logic (remains mostly the same, but using IElement)
        var functions = nodes.Select(node =>
            (Func<Task<VideoServer>>)(
                async () => await GetVideoServerAsync(node, cancellationToken)
            )
        );

        list.AddRange(await TaskHelper.Run(functions, 10));

        return list;
    }

    private async ValueTask<VideoServer> GetVideoServerAsync(
        IElement node,
        CancellationToken cancellationToken = default
    )
    {
        // 1. Get the server ID from the attribute
        // HAP: node.Attributes["data-link-id"].Value
        var serverId = node.GetAttribute("data-link-id");

        if (string.IsNullOrWhiteSpace(serverId))
            return new VideoServer();

        // 2. Encode VRF and fetch the server details
        var vrf2 = await EncodeVrfAsync(serverId, cancellationToken);
        var response3 = await HttpClient.ExecuteAsync(
            $"{BaseUrl}/ajax/server/{serverId}?vrf={vrf2}",
            cancellationToken
        );

        // 3. Extract the encrypted URL from JSON
        var encodedStreamUrl = JsonNode.Parse(response3)?["result"]?["url"]?.ToString();
        if (string.IsNullOrWhiteSpace(encodedStreamUrl))
            return new();

        // 4. Decode the final stream link
        var realLink = await DecodeVrfAsync(encodedStreamUrl, cancellationToken);

        return new VideoServer
        {
            // HAP: node.InnerText
            Name = node.TextContent.Trim(),
            Embed = new FileUrl(realLink)
            {
                Headers = new Dictionary<string, string> { ["Referer"] = BaseUrl },
            },
        };
    }

    public override IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        return server.Name.ToLower() switch
        {
            "vidstream" => new AniwaveExtractor(HttpClientFactory, "VidStream"),
            "mycloud" => new AniwaveExtractor(HttpClientFactory, "MyCloud"),
            "filemoon" => new FilemoonExtractor(HttpClientFactory),
            "streamtape" => new StreamTapeExtractor(HttpClientFactory),
            "mp4upload" => new Mp4UploadExtractor(HttpClientFactory),
            _ => null,
        };
    }

    /// <summary>
    /// Encodes a string by making an http request to <see href="https://9anime.eltik.net"/>.
    /// </summary>
    /// <param name="query">The string to encode.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An encoded string.</returns>
    public async ValueTask<string> EncodeVrfAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var response = await HttpClient.ExecuteAsync(
            $"https://9anime.eltik.net/vrf?query={query}&apikey=chayce",
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;

        var data = JsonNode.Parse(response)!;

        var vrf = data["url"]?.ToString();

        if (!string.IsNullOrWhiteSpace(vrf))
            return vrf!;

        return string.Empty;
    }

    /// <summary>
    /// Decodes a string by making an http request to <see href="https://9anime.eltik.net"/>.
    /// </summary>
    /// <param name="query">The string to decode.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A decoded string.</returns>
    public async ValueTask<string> DecodeVrfAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var response = await HttpClient.ExecuteAsync(
            $"https://9anime.eltik.net/decrypt?query={query}&apikey=chayce",
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;

        var data = JsonNode.Parse(response)!;

        var vrf = data["url"]?.ToString();

        if (!string.IsNullOrWhiteSpace(vrf))
            return vrf!;

        return string.Empty;
    }
}
