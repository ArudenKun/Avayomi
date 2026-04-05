using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Avayomi.Core;
using Avayomi.Core.AniList;
using Avayomi.Core.Anime;
using Avayomi.Core.Extensions;
using Avayomi.Core.Providers.Anime;
using Avayomi.Core.Videos;
using Avayomi.Extractors;

namespace Avayomi.Providers.Anime;

/// <summary>
/// Client for interacting with AniKai.to (AnimeKai).
/// This is a standalone provider — not ZoroTheme-based.
/// </summary>
public class AniKai : AnimeBaseProvider, IAnimeProvider
{
    private readonly MegaUpExtractor _megaUp;

    public string Key => Name;
    public string Name => "AniKai";
    public string Language => "en";
    public string BaseUrl => "https://anikai.to";
    public bool IsDubAvailableSeparately => false;

    /// <summary>
    /// Initializes an instance of <see cref="AniKai"/>.
    /// </summary>
    public AniKai(IHttpClientFactory httpClientFactory, IAniListClient aniListClient)
        : base(httpClientFactory, aniListClient)
    {
        _megaUp = new MegaUpExtractor(httpClientFactory);
    }

    #region Headers

    private Dictionary<string, string> GetHeaders()
    {
        var uri = new Uri(BaseUrl);
        return new Dictionary<string, string>
        {
            ["Accept"] = "text/html, */*; q=0.01",
            ["Accept-Language"] = "en-US,en;q=0.5",
            ["Cache-Control"] = "no-cache",
            ["Connection"] = "keep-alive",
            ["Cookie"] = "__p_mov=1; usertype=guest",
            ["Host"] = uri.Host,
            ["Pragma"] = "no-cache",
            ["Priority"] = "u=0",
            ["Referer"] = $"{BaseUrl}/",
            ["Sec-Fetch-Dest"] = "empty",
            ["Sec-Fetch-Mode"] = "cors",
            ["Sec-Fetch-Site"] = "same-origin",
            ["Sec-GPC"] = "1",
        };
    }

    private Dictionary<string, string> GetAjaxHeaders(string referer)
    {
        var headers = GetHeaders();
        headers["X-Requested-With"] = "XMLHttpRequest";
        headers["Referer"] = referer;
        return headers;
    }

    #endregion

    #region Search

    /// <inheritdoc />
    public async ValueTask<List<AnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var keyword = Regex.Replace(query, @"[\W_]+", "+");
        var url = $"{BaseUrl}/browser?keyword={keyword}";
        var response = await HttpClient.ExecuteAsync(url, GetHeaders(), cancellationToken);
        return ParseAnimeList(response);
    }

    private List<AnimeInfo> ParseAnimeList(string? response)
    {
        var list = new List<AnimeInfo>();
        if (string.IsNullOrWhiteSpace(response))
            return list;

        // Assuming your HtmlHelper.Parse returns an AngleSharp IHtmlDocument
        var document = HtmlHelper.Parse(response);

        // Use QuerySelectorAll with a CSS class selector
        var nodes = document.QuerySelectorAll("div.aitem");

        foreach (var node in nodes)
        {
            // AngleSharp nodes are IElement, ensure ParseAnimeCard is updated to accept it
            var anime = ParseAnimeCard(node);
            if (anime is not null)
                list.Add(anime);
        }

        return list;
    }

    private AnimeInfo? ParseAnimeCard(IElement node)
    {
        // 1. Get the poster link
        var posterLink = node.QuerySelector("a.poster");
        if (posterLink is null)
            return null;

        var href = posterLink.GetAttribute("href");
        if (string.IsNullOrWhiteSpace(href))
            return null;

        var id = href.Replace("/watch/", "");

        // 2. Get Title and Japanese Title
        var titleNode = node.QuerySelector("a.title");
        var title = titleNode?.GetAttribute("title") ?? titleNode?.TextContent.Trim() ?? "";
        var japaneseTitle = titleNode?.GetAttribute("data-jp");

        // 3. Get Image (checks data-src first, then src)
        var imgNode = node.QuerySelector("img");
        var image = imgNode?.GetAttribute("data-src") ?? imgNode?.GetAttribute("src");

        // 4. Get Type (e.g., Movie, TV)
        // HAP: .//div[contains(@class, 'info')]//span/b -> Last
        // AngleSharp: Targeted CSS selector
        var type = node.QuerySelector("div.info span b:last-of-type")?.TextContent.Trim();

        return new AnimeInfo(id)
        {
            Title = title,
            OtherNames = japaneseTitle,
            Image = image,
            Type = type,
            Link = $"{BaseUrl}{href}",
        };
    }

    #endregion

    #region Anime Info

    /// <inheritdoc />
    public async ValueTask<AnimeInfo> GetAnimeInfoAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        var slug = animeId.Split('$')[0];
        var url = slug.StartsWith("http") ? slug : $"{BaseUrl}/watch/{slug}";
        var response = await HttpClient.ExecuteAsync(url, GetHeaders(), cancellationToken);
        var document = HtmlHelper.Parse(response);

        var anime = new AnimeInfo(slug) { Link = url };

        // 1. Title and Japanese Title
        var titleNode = document.QuerySelector("div.entity-scroll h1.title");
        anime.Title = titleNode?.TextContent.Trim() ?? "";
        anime.OtherNames = titleNode?.GetAttribute("data-jp");

        // 2. Image
        anime.Image = document.QuerySelector("div.poster img")?.GetAttribute("src");

        // 3. Description
        anime.Summary = document.QuerySelector("div.entity-scroll div.desc")?.TextContent.Trim();

        // 4. Type (Targeting the last <b> inside info spans)
        anime.Type = document
            .QuerySelector("div.entity-scroll div.info span b:last-of-type")
            ?.TextContent.Trim();

        // 5. Detail Section
        // Instead of looping every div, we can target specific labels directly if needed,
        // but a loop is safer if the order changes.
        var detailNodes = document.QuerySelectorAll("div.detail > div > div");
        foreach (var div in detailNodes)
        {
            var text = div.TextContent.Trim();
            var value = div.QuerySelector("span")?.TextContent.Trim() ?? "";

            if (text.StartsWith("Status:"))
                anime.Status = value;
            else if (text.StartsWith("Date aired:"))
                anime.Released = value;
            else if (text.StartsWith("Studios:"))
                anime.Category = value;
            else if (text.StartsWith("Genres:"))
            {
                anime.Genres = div.QuerySelectorAll("a")
                    .Select(g => new Genre(g.TextContent.Trim()))
                    .ToList();
            }
        }

        return anime;
    }

    #endregion

    #region Episodes

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        var slug = animeId.Split('$')[0];
        var pageUrl = $"{BaseUrl}/watch/{slug}";

        // 1. Fetch initial page
        var pageResponse = await HttpClient.ExecuteAsync(pageUrl, GetHeaders(), cancellationToken);
        if (string.IsNullOrWhiteSpace(pageResponse))
            return [];

        var document = HtmlHelper.Parse(pageResponse);

        // 2. Extract ani_id (CSS selector for ID and Attribute)
        var aniId = document.QuerySelector("#anime-rating[data-id]")?.GetAttribute("data-id");

        // Fallback: Regex remains the same
        if (string.IsNullOrWhiteSpace(aniId))
        {
            var match = Regex.Match(
                pageResponse,
                """
                "anime_id"\s*:\s*"([^"]+)"
                """
            );
            if (match.Success)
                aniId = match.Groups[1].Value;
        }

        if (string.IsNullOrWhiteSpace(aniId))
            return [];

        // 3. Generate token and fetch AJAX response
        var token = await _megaUp.GenerateTokenAsync(aniId, cancellationToken);
        var ajaxUrl = $"{BaseUrl}/ajax/episodes/list?ani_id={aniId}&_={token}";
        var ajaxResponse = await HttpClient.ExecuteAsync(
            ajaxUrl,
            GetAjaxHeaders(pageUrl),
            cancellationToken
        );

        var html = ParseResultHtml(ajaxResponse);
        if (string.IsNullOrWhiteSpace(html))
            return [];

        // 4. Parse the AJAX HTML fragment
        var epDocument = HtmlHelper.Parse(html);
        var epNodes = epDocument.QuerySelectorAll("div.eplist a");

        var episodes = new List<Episode>();
        foreach (var epNode in epNodes)
        {
            var num = epNode.GetAttribute("num") ?? "1";
            var epToken = epNode.GetAttribute("token") ?? "";
            var epTitle = epNode.QuerySelector("span")?.TextContent.Trim() ?? "";

            // Checking for a class is easier with ClassList
            var isFiller = epNode.ClassList.Contains("filler");

            var episodeNumber = float.TryParse(num, out var n) ? n : 1f;
            var episodeId = $"{slug}$ep={num}$token={epToken}";

            var episode = new Episode
            {
                Id = episodeId,
                Number = episodeNumber,
                Name = string.IsNullOrWhiteSpace(epTitle)
                    ? $"Episode {episodeNumber}"
                    : $"Ep. {episodeNumber}: {epTitle}",
                Link = $"{BaseUrl}/watch/{slug}?ep={num}",
            };

            if (isFiller)
                episode.Description = "Filler Episode";

            episodes.Add(episode);
        }

        return episodes;
    }

    #endregion

    #region Video Servers

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        // 1. Extract token from ID
        var parts = episodeId.Split(["$token="], StringSplitOptions.None);
        var epToken = parts.Length >= 2 ? parts[1] : "";

        if (string.IsNullOrWhiteSpace(epToken))
            return [];

        // 2. Fetch server list via AJAX
        var token = await _megaUp.GenerateTokenAsync(epToken, cancellationToken);
        var ajaxUrl = $"{BaseUrl}/ajax/links/list?token={epToken}&_={token}";
        var referer = $"{BaseUrl}/watch/{episodeId.Split('$')[0]}";

        var response = await HttpClient.ExecuteAsync(
            ajaxUrl,
            GetAjaxHeaders(referer),
            cancellationToken
        );

        var html = ParseResultHtml(response);
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var document = HtmlHelper.Parse(html);
        var servers = new List<VideoServer>();

        // 3. Parse server groups (softsub, dub, raw)
        // HAP: //div[contains(@class, 'server-items')]
        var serverGroups = document.QuerySelectorAll("div.server-items");

        foreach (var group in serverGroups)
        {
            var groupType = (group.GetAttribute("data-id") ?? "softsub").ToUpperInvariant();

            // HAP: .//span[contains(@class, 'server')]
            var serverNodes = group.QuerySelectorAll("span.server");

            foreach (var serverNode in serverNodes)
            {
                var linkId = serverNode.GetAttribute("data-lid");
                var serverName = serverNode.TextContent.Trim();

                if (string.IsNullOrWhiteSpace(linkId))
                    continue;

                // 4. Fetch the actual embed URL for this server
                var linkToken = await _megaUp.GenerateTokenAsync(linkId, cancellationToken);
                var linkUrl = $"{BaseUrl}/ajax/links/view?id={linkId}&_={linkToken}";
                var linkResponse = await HttpClient.ExecuteAsync(
                    linkUrl,
                    GetAjaxHeaders(referer),
                    cancellationToken
                );

                var linkResult = ParseResultString(linkResponse);
                if (string.IsNullOrWhiteSpace(linkResult))
                    continue;

                // 5. Decode the iframe data
                var (videoUrl, _, _) = await _megaUp.DecodeIframeDataAsync(
                    linkResult,
                    cancellationToken
                );

                if (string.IsNullOrWhiteSpace(videoUrl))
                    continue;

                servers.Add(
                    new VideoServer
                    {
                        Name = $"{serverName} ({groupType})",
                        Embed = new FileUrl(videoUrl)
                        {
                            Headers = new Dictionary<string, string> { ["Referer"] = BaseUrl },
                        },
                    }
                );
            }
        }

        return servers;
    }

    /// <inheritdoc />
    public override IVideoExtractor GetVideoExtractor(VideoServer server)
    {
        // All AniKai servers use MegaUp
        return new MegaUpExtractor(HttpClientFactory);
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Parses the "result" field from a JSON AJAX response as HTML content.
    /// Handles both raw JSON and HTML-wrapped JSON responses.
    /// </summary>
    private static string? ParseResultHtml(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        try
        {
            // 1. Try parsing as raw JSON first
            var json = JsonNode.Parse(response);
            return json?["result"]?.ToString();
        }
        catch
        {
            // 2. If it's HTML, look for JSON inside a <pre> tag
            var trimmed = response.TrimStart();
            if (trimmed.StartsWith("<"))
            {
                // Parse the HTML fragment
                var doc = HtmlHelper.Parse(response);
                var pre = doc.QuerySelector("pre");

                if (pre is not null)
                {
                    try
                    {
                        // Use TextContent to get the raw string inside the <pre> tag
                        var json = JsonNode.Parse(pre.TextContent);
                        return json?["result"]?.ToString();
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Parses the "result" field from a JSON AJAX response as a string value.
    /// </summary>
    private static string? ParseResultString(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        try
        {
            // 1. Attempt to parse raw JSON
            var json = JsonNode.Parse(response);
            return json?["result"]?.ToString();
        }
        catch
        {
            // 2. Check if the response is an HTML wrapper (starts with '<')
            var trimmed = response.TrimStart();
            if (trimmed.StartsWith("<"))
            {
                var doc = HtmlHelper.Parse(response);

                // CSS selector 'pre' instead of XPath '//pre'
                var pre = doc.QuerySelector("pre");

                if (pre is not null)
                {
                    try
                    {
                        // Use TextContent to extract the string inside the <pre> tag
                        var json = JsonNode.Parse(pre.TextContent);
                        return json?["result"]?.ToString();
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            return null;
        }
    }

    #endregion
}
