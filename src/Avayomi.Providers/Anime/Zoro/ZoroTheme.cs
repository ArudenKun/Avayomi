using System.Text.Json.Nodes;
using AngleSharp.Dom;
using Avayomi.Core;
using Avayomi.Core.AniList;
using Avayomi.Core.Anime;
using Avayomi.Core.Extensions;
using Avayomi.Core.Providers.Anime;
using Avayomi.Core.Videos;
using Avayomi.Extractors;

namespace Avayomi.Providers.Anime.Zoro;

/// <summary>
/// Abstract base class for ZoroTheme-based anime providers.
/// </summary>
public abstract class ZoroTheme
    : AnimeBaseProvider,
        IAnimeProvider,
        IPopularProvider,
        ILastUpdatedProvider
{
    protected ZoroTheme(IHttpClientFactory httpClientFactory, IAniListClient aniListClient)
        : base(httpClientFactory, aniListClient) { }

    public abstract string Key { get; }
    public abstract string Name { get; }
    public abstract string Language { get; }
    public abstract string BaseUrl { get; }

    public virtual bool IsDubAvailableSeparately => false;

    /// <summary>
    /// List of supported hoster names for this provider.
    /// </summary>
    protected abstract List<string> HosterNames { get; }

    /// <summary>
    /// Optional ajax route prefix (e.g., "/v2" for some sites).
    /// </summary>
    protected virtual string AjaxRoute => "";

    /// <summary>
    /// Whether to use English titles instead of Romaji.
    /// </summary>
    public bool UseEnglishTitles { get; set; } = false;

    /// <summary>
    /// Whether to mark filler episodes.
    /// </summary>
    public bool MarkFillerEpisodes { get; set; } = true;

    /// <summary>
    /// Preferred video quality (e.g., "1080", "720").
    /// </summary>
    public string PreferredQuality { get; set; } = "1080";

    /// <summary>
    /// Preferred video type (e.g., "Sub", "Dub").
    /// </summary>
    public string PreferredType { get; set; } = "Sub";

    /// <summary>
    /// Preferred server name.
    /// </summary>
    public string? PreferredServer { get; set; }

    /// <summary>
    /// Enabled hosters for video extraction.
    /// </summary>
    public HashSet<string> EnabledHosters { get; set; } = [];

    /// <summary>
    /// Enabled video types (servers-sub, servers-dub, servers-mixed, servers-raw).
    /// </summary>
    public HashSet<string> EnabledTypes { get; set; } =
    ["servers-sub", "servers-dub", "servers-mixed", "servers-raw"];

    #region Request Headers

    protected virtual Dictionary<string, string> GetDocHeaders()
    {
        var uri = new Uri(BaseUrl);
        return new Dictionary<string, string>
        {
            ["Accept"] =
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
            ["Host"] = uri.Host,
            ["Referer"] = $"{BaseUrl}/",
        };
    }

    protected virtual Dictionary<string, string> GetApiHeaders(string referer)
    {
        var uri = new Uri(BaseUrl);
        return new Dictionary<string, string>
        {
            ["Accept"] = "*/*",
            ["Host"] = uri.Host,
            ["Referer"] = referer,
            ["X-Requested-With"] = "XMLHttpRequest",
        };
    }

    #endregion

    #region Popular Anime

    /// <inheritdoc />
    public virtual async ValueTask<List<AnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"{BaseUrl}/most-popular?page={page}";
        var response = await HttpClient.ExecuteAsync(url, GetDocHeaders(), cancellationToken);
        return ParseAnimeList(response);
    }

    #endregion

    #region Latest Updates

    /// <inheritdoc />
    public virtual async ValueTask<List<AnimeInfo>> GetLastUpdatedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"{BaseUrl}/top-airing?page={page}";
        var response = await HttpClient.ExecuteAsync(url, GetDocHeaders(), cancellationToken);
        return ParseAnimeList(response);
    }

    #endregion

    #region Search

    /// <inheritdoc />
    public virtual async ValueTask<List<AnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        return await SearchAsync(query, new ZoroThemeSearchParameters(), cancellationToken);
    }

    /// <summary>
    /// Searches for anime with advanced filter parameters.
    /// </summary>
    public virtual async ValueTask<List<AnimeInfo>> SearchAsync(
        string query,
        ZoroThemeSearchParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var endpoint = string.IsNullOrWhiteSpace(query) ? "filter" : "search";
        var url = BuildSearchUrl(endpoint, query, parameters);

        var response = await HttpClient.ExecuteAsync(url, GetDocHeaders(), cancellationToken);
        return ParseAnimeList(response);
    }

    private string BuildSearchUrl(string endpoint, string query, ZoroThemeSearchParameters p)
    {
        var queryParams = new List<string> { $"page={p.Page}" };

        AddIfNotBlank(queryParams, "keyword", query);
        AddIfNotBlank(queryParams, "type", p.Type);
        AddIfNotBlank(queryParams, "status", p.Status);
        AddIfNotBlank(queryParams, "rated", p.Rated);
        AddIfNotBlank(queryParams, "score", p.Score);
        AddIfNotBlank(queryParams, "season", p.Season);
        AddIfNotBlank(queryParams, "language", p.Language);
        AddIfNotBlank(queryParams, "sort", p.Sort);
        AddIfNotBlank(queryParams, "sy", p.StartYear);
        AddIfNotBlank(queryParams, "sm", p.StartMonth);
        AddIfNotBlank(queryParams, "sd", p.StartDay);
        AddIfNotBlank(queryParams, "ey", p.EndYear);
        AddIfNotBlank(queryParams, "em", p.EndMonth);
        AddIfNotBlank(queryParams, "ed", p.EndDay);
        AddIfNotBlank(queryParams, "genres", p.Genres);

        return $"{BaseUrl}/{endpoint}?{string.Join("&", queryParams)}";
    }

    private static void AddIfNotBlank(List<string> queryParams, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            queryParams.Add($"{key}={Uri.EscapeDataString(value)}");
        }
    }

    #endregion

    #region Anime Parsing

    private List<AnimeInfo> ParseAnimeList(string? response)
    {
        var list = new List<AnimeInfo>();

        if (string.IsNullOrWhiteSpace(response))
            return list;

        // Assuming HtmlHelper.Parse returns an IHtmlDocument
        var document = HtmlHelper.Parse(response);

        // CSS selector replaces the //div[contains(@class, 'flw-item')] XPath
        var nodes = document.QuerySelectorAll("div.flw-item");

        foreach (var node in nodes)
        {
            // Ensure ParseAnimeFromElement is updated to accept AngleSharp's IElement
            var anime = ParseAnimeFromElement(node);
            if (anime is not null)
                list.Add(anime);
        }

        return list;
    }

    private AnimeInfo? ParseAnimeFromElement(IElement node)
    {
        // 1. Get the link/title node using CSS breadcrumbs
        // HAP: .//div[contains(@class, 'film-detail')]//a
        var linkNode = node.QuerySelector("div.film-detail a");
        if (linkNode is null)
            return null;

        var href = linkNode.GetAttribute("href") ?? "";

        // 2. Extract Title based on your preference setting
        // AngleSharp: GetAttribute("title") returns null if missing, making ?? logic clean
        var title =
            UseEnglishTitles && linkNode.HasAttribute("title")
                ? linkNode.GetAttribute("title")
                : linkNode.GetAttribute("data-jname");

        // 3. Get the image from the poster div
        // HAP: .//div[contains(@class, 'film-poster')]//img
        var imgNode = node.QuerySelector("div.film-poster img");
        var image = imgNode?.GetAttribute("data-src") ?? imgNode?.GetAttribute("src");

        return new AnimeInfo(href)
        {
            Title = title ?? "",
            Image = image,
            Link = $"{BaseUrl}{href}",
        };
    }

    #endregion

    #region Anime Details

    /// <inheritdoc />
    public virtual async ValueTask<AnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var url = id.StartsWith("http") ? id : $"{BaseUrl}{id}";
        var response = await HttpClient.ExecuteAsync(url, GetDocHeaders(), cancellationToken);
        var document = HtmlHelper.Parse(response);

        var anime = new AnimeInfo(id) { Link = url };

        // 1. Thumbnail (Poster Image)
        // CSS Selector: div.anisc-poster img
        anime.Image = document.QuerySelector("div.anisc-poster img")?.GetAttribute("src");

        // 2. Info Section
        var infoNode = document.QuerySelector("div.anisc-info");
        if (infoNode is not null)
        {
            // Title: Checking h2 then h1
            anime.Title =
                infoNode.QuerySelector("h2")?.TextContent?.Trim()
                ?? infoNode.QuerySelector("h1")?.TextContent?.Trim()
                ?? "";

            // Metadata extraction
            anime.Summary = GetInfoValue(infoNode, "Overview:");
            anime.Status = GetInfoValue(infoNode, "Status:");
            anime.Released = GetInfoValue(infoNode, "Aired:");
            anime.Type = GetInfoValue(infoNode, "Type:");
            anime.OtherNames =
                GetInfoValue(infoNode, "Synonyms:") ?? GetInfoValue(infoNode, "Japanese:");

            // Studios
            anime.Category = GetInfoValue(infoNode, "Studios:");

            // Genres: Targeting links within a div containing the text "Genres:"
            // Note: AngleSharp doesn't have a CSS :contains selector, so we filter the elements
            var genreContainer = infoNode
                .QuerySelectorAll("div.item-list")
                .FirstOrDefault(x => x.TextContent.Contains("Genres:"));

            if (genreContainer != null)
            {
                anime.Genres = genreContainer
                    .QuerySelectorAll("a")
                    .Select(g => new Genre(g.TextContent.Trim()))
                    .ToList();
            }
        }

        return anime;
    }

    private static string? GetInfoText(IElement infoNode, string tag)
    {
        // HAP: .//{tag}
        // AngleSharp: Targets the first occurrence of the tag within this node
        return infoNode.QuerySelector(tag)?.TextContent.Trim();
    }

    private static string? GetInfoValue(IElement infoNode, string label)
    {
        // 1. Find the 'item' div that contains the specific label text
        // HAP: .//div[contains(@class, 'item-title')][contains(., '{label}')]
        var itemNode = infoNode
            .QuerySelectorAll("div.item-title")
            .FirstOrDefault(x => x.TextContent.Contains(label, StringComparison.OrdinalIgnoreCase));

        if (itemNode is null)
            return null;

        // 2. Find the value node (class contains 'name' or 'text')
        // HAP: .//*[contains(@class, 'name') or contains(@class, 'text')]
        // AngleSharp: Use a comma in QuerySelector to act as an "OR" operator
        var valueNode = itemNode.QuerySelector(".name, .text");

        return valueNode?.TextContent?.Trim();
    }

    #endregion

    #region Episodes

    /// <inheritdoc />
    public virtual async ValueTask<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var animePageUrl = id.StartsWith("http") ? id : $"{BaseUrl}{id}";
        var animePageResponse = await HttpClient.ExecuteAsync(
            animePageUrl,
            GetDocHeaders(),
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(animePageResponse))
            return [];

        var animeDocument = HtmlHelper.Parse(animePageResponse);

        // 1. Extract data-id using a CSS selector for any element with that attribute
        var dataId = animeDocument.QuerySelector("[data-id]")?.GetAttribute("data-id");

        if (string.IsNullOrWhiteSpace(dataId))
        {
            dataId = ExtractAnimeSlugFromUrl(id);
        }

        // 2. Fetch the AJAX response
        var url = $"{BaseUrl}/ajax{AjaxRoute}/episode/list/{dataId}";
        var response = await HttpClient.ExecuteAsync(
            url,
            GetApiHeaders(animePageUrl),
            cancellationToken
        );

        var html = ParseHtmlFromJson(response);
        if (string.IsNullOrWhiteSpace(html))
            return [];

        // 3. Parse the AJAX HTML fragment
        var document = HtmlHelper.Parse(html);
        var nodes = document.QuerySelectorAll("a.ep-item");

        var episodes = new List<Episode>();

        foreach (var node in nodes)
        {
            // Extracting data-attributes
            var numStr = node.GetAttribute("data-number") ?? "1";
            var episodeNumber = float.TryParse(numStr, out var n) ? n : 1f;

            var episodeTitle = node.GetAttribute("title") ?? "";
            var episodeHref = node.GetAttribute("href") ?? "";

            // Use ClassList for cleaner boolean checks
            var isFiller = node.ClassList.Contains("ssl-item-filler");

            var episode = new Episode
            {
                Id = episodeHref,
                Number = episodeNumber,
                Name = $"Ep. {episodeNumber}: {episodeTitle}",
                Link = $"{BaseUrl}{episodeHref}",
            };

            if (isFiller && MarkFillerEpisodes)
            {
                episode.Description = "Filler Episode";
            }

            episodes.Add(episode);
        }

        // Return in ascending order
        episodes.Reverse();
        return episodes;
    }

    /// <summary>
    /// Extracts the anime slug from a URL (e.g., "steinsgate-3" from "/watch/steinsgate-3").
    /// </summary>
    protected static string ExtractAnimeSlugFromUrl(string url)
    {
        var path = url.Split('?')[0];

        // Remove leading slashes and "watch/" prefix if present
        path = path.TrimStart('/');
        if (path.StartsWith("watch/"))
            path = path.Substring(6);

        // Return the slug (last path segment)
        var lastSlashIndex = path.LastIndexOf('/');
        return lastSlashIndex >= 0 ? path.Substring(lastSlashIndex + 1) : path;
    }

    /// <summary>
    /// Extracts the numeric ID from the end of a URL path.
    /// </summary>
    protected static string ExtractNumericIdFromUrl(string url)
    {
        var path = url.Split('?')[0];
        var lastDashIndex = path.LastIndexOf('-');
        return lastDashIndex >= 0 ? path.Substring(lastDashIndex + 1) : path;
    }

    #endregion

    #region Video Servers

    /// <inheritdoc />
    public virtual async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        // 1. Extract the numeric ID from the URL or parameter
        var epId = episodeId.Contains("?ep=")
            ? episodeId.Split(new[] { "?ep=" }, StringSplitOptions.None).Last()
            : ExtractNumericIdFromUrl(episodeId);

        var referer = episodeId.StartsWith("http") ? episodeId : $"{BaseUrl}{episodeId}";

        // 2. Fetch the server list via AJAX
        var url = $"{BaseUrl}/ajax{AjaxRoute}/episode/servers?episodeId={epId}";
        var response = await HttpClient.ExecuteAsync(
            url,
            GetApiHeaders(referer),
            cancellationToken
        );

        var html = ParseHtmlFromJson(response);
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var document = HtmlHelper.Parse(html);
        var servers = new List<VideoServer>();

        var serverTypes = new[] { "servers-sub", "servers-dub", "servers-mixed", "servers-raw" };

        foreach (var serverType in serverTypes)
        {
            if (!EnabledTypes.Contains(serverType))
                continue;

            var typeLabel = serverType.Replace("servers-", "").ToUpperInvariant();

            // HAP: //div[contains(@class, '{serverType}')]//div[contains(@class, 'item')]
            // AngleSharp: Targeted CSS breadcrumbs
            var serverNodes = document.QuerySelectorAll($"div.{serverType} div.item");

            foreach (var serverNode in serverNodes)
            {
                var serverId = serverNode.GetAttribute("data-id");
                var serverName = serverNode.TextContent.Trim();

                if (string.IsNullOrWhiteSpace(serverId) || !IsHosterEnabled(serverName))
                    continue;

                // 3. Fetch the actual embed URL
                var embedUrl = await GetServerEmbedUrlAsync(serverId, referer, cancellationToken);
                if (string.IsNullOrWhiteSpace(embedUrl))
                    continue;

                servers.Add(
                    new VideoServer
                    {
                        Name = $"{serverName} ({typeLabel})",
                        Embed = new FileUrl(embedUrl)
                        {
                            Headers = new Dictionary<string, string> { ["Referer"] = BaseUrl },
                        },
                    }
                );
            }
        }

        return servers;
    }

    private async ValueTask<string?> GetServerEmbedUrlAsync(
        string serverId,
        string referer,
        CancellationToken cancellationToken
    )
    {
        var url = $"{BaseUrl}/ajax{AjaxRoute}/episode/sources?id={serverId}";
        var response = await HttpClient.ExecuteAsync(
            url,
            GetApiHeaders(referer),
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response))
            return null;

        try
        {
            var json = JsonNode.Parse(response);
            return json?["link"]?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private bool IsHosterEnabled(string hosterName)
    {
        if (EnabledHosters.Count == 0)
        {
            // If no specific hosters are enabled, use all available hosters
#if NETCOREAPP
            EnabledHosters = HosterNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
#else
            EnabledHosters = new HashSet<string>(HosterNames, StringComparer.OrdinalIgnoreCase);
#endif
        }

        return EnabledHosters.Any(h => h.Equals(hosterName, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Video Extraction

    /// <summary>
    /// Override this method to provide custom video extractors for specific servers.
    /// </summary>
    public override IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        // Default implementation - subclasses should override for custom extractors
        return base.GetVideoExtractor(server);
    }

    /// <summary>
    /// Sorts videos based on user preferences.
    /// </summary>
    public virtual List<VideoSource> SortVideos(List<VideoSource> videos)
    {
        return videos
            .OrderByDescending(v => v.Resolution?.Contains(PreferredQuality) == true)
            .ThenByDescending(v =>
                v.VideoServer?.Name.Contains(
                    PreferredServer ?? "",
                    StringComparison.OrdinalIgnoreCase
                ) == true
            )
            .ThenByDescending(v =>
                v.VideoServer?.Name.Contains(PreferredType, StringComparison.OrdinalIgnoreCase)
                == true
            )
            .ToList();
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Parses the HTML content from a JSON response with an "html" field.
    /// </summary>
    protected static string? ParseHtmlFromJson(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        try
        {
            var json = JsonNode.Parse(response);
            return json?["html"]?.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses anime status string to standardized status.
    /// </summary>
    protected static string ParseStatus(string? statusString)
    {
        return statusString switch
        {
            "Currently Airing" => "Ongoing",
            "Finished Airing" => "Completed",
            _ => "Unknown",
        };
    }

    #endregion
}
