// using System.Text.Json.Nodes;
// using Avayomi.Core;
// using Avayomi.Core.Anime;
// using Avayomi.Core.Extensions;
// using Avayomi.Core.Providers.Anime;
// using Avayomi.Core.Tasks;
// using Avayomi.Core.Videos;
// using Avayomi.Extractors;
//
// namespace Avayomi.Providers.Anime;
//
// /// <summary>
// /// Client for interacting with 9anime.
// /// </summary>
// /// <remarks>
// /// Initializes an instance of <see cref="NineAnime"/>.
// /// </remarks>
// public class NineAnime : AnimeBaseProvider, IAnimeProvider, IPopularProvider, ILastUpdatedProvider
// {
//     /// <summary>
//     /// Client for interacting with 9anime.
//     /// </summary>
//     /// <remarks>
//     /// Initializes an instance of <see cref="NineAnime"/>.
//     /// </remarks>
//     public NineAnime(IHttpClientFactory httpClientFactory)
//         : base(httpClientFactory) { }
//
//     public string Key => Name;
//
//     public string Name => "NineAnime";
//
//     public string Language => "en";
//
//     public bool IsDubAvailableSeparately => false;
//
//     public string BaseUrl => "https://9anime.pe";
//
//     /// <inheritdoc />
//     public async ValueTask<List<IAnimeInfo>> SearchAsync(
//         string query,
//         CancellationToken cancellationToken = default
//     )
//     {
//         var response = await HttpClient.ExecuteAsync(
//             $"{BaseUrl}/filter?keyword={Uri.EscapeDataString(query).Replace("%20", "+")}",
//             new Dictionary<string, string> { ["Referer"] = BaseUrl },
//             cancellationToken
//         );
//
//         return ParseAnimeSearchResponse(response);
//     }
//
//     /// <inheritdoc cref="SearchAsync"/>
//     public async ValueTask<List<IAnimeInfo>> GetPopularAsync(
//         int page = 1,
//         CancellationToken cancellationToken = default
//     )
//     {
//         var response = await HttpClient.ExecuteAsync(
//             $"{BaseUrl}/filter?sort=trending&page={page}",
//             cancellationToken
//         );
//
//         return ParseAnimeResponse(response);
//     }
//
//     /// <summary>
//     /// Gets anime in new season.
//     /// </summary>
//     /// <param name="page"></param>
//     /// <param name="cancellationToken"></param>
//     public async ValueTask<List<IAnimeInfo>> GetLastUpdatedAsync(
//         int page = 1,
//         CancellationToken cancellationToken = default
//     )
//     {
//         var http = HttpClientFactory.CreateProviderHttpClient();
//         var response = await http.ExecuteAsync(
//             $"{BaseUrl}/filter?sort=recently_updated&page={page}",
//             cancellationToken
//         );
//
//         return ParseAnimeResponse(response);
//     }
//
//     private List<IAnimeInfo> ParseAnimeResponse(string? response)
//     {
//         var list = new List<IAnimeInfo>();
//
//         if (string.IsNullOrWhiteSpace(response))
//             return list;
//
//         var document = HtmlHelper.Parse(response);
//
//         // Target items inside the list-items container directly
//         var nodes = document.QuerySelectorAll("#list-items > div.item");
//
//         foreach (var node in nodes)
//         {
//             // 1. Get the anchor for ID and link info
//             var aniAnchor = node.QuerySelector("div.ani a");
//             var href = aniAnchor?.GetAttribute("href") ?? "";
//
//             // 2. Get the title anchor
//             var titleAnchor = node.QuerySelector("div.info div.b1 a");
//
//             var animeInfo = new AnimeInfo
//             {
//                 Site = AnimeSites.Aniwave,
//                 Id = href, // You can still .Split('/') here if needed
//                 Title = titleAnchor?.TextContent?.Trim() ?? "",
//                 Image = node.QuerySelector("div.ani a img")?.GetAttribute("src") ?? "",
//             };
//
//             list.Add(animeInfo);
//         }
//
//         return list;
//     }
//
//     private List<IAnimeInfo> ParseAnimeSearchResponse(string? response)
//     {
//         var list = new List<IAnimeInfo>();
//         if (string.IsNullOrWhiteSpace(response))
//             return list;
//
//         // 1. Extract HTML from the JSON result
//         var data = JsonNode.Parse(response);
//         var html = data?["result"]?["html"]?.ToString();
//         if (string.IsNullOrWhiteSpace(html))
//             return list;
//
//         var document = HtmlHelper.Parse(html);
//
//         // 2. Target the search result anchors
//         var nodes = document.QuerySelectorAll("a.item");
//
//         foreach (var node in nodes)
//         {
//             var animeInfo = new AnimeInfo
//             {
//                 Site = AnimeSites.Aniwave,
//                 Id = node.GetAttribute("href") ?? "",
//                 Title = node.QuerySelector("div.name.d-title")?.TextContent.Trim() ?? "",
//                 Image = node.QuerySelector("img")?.GetAttribute("src") ?? "",
//
//                 // XPath: span[last()] -> CSS: span:last-child
//                 Released = node.QuerySelector("div.meta span:last-child")?.TextContent.Trim() ?? "",
//
//                 // XPath: span[last()-1] -> CSS: span:nth-last-child(2)
//                 Type =
//                     node.QuerySelector("div.meta span:nth-last-child(2)")?.TextContent.Trim() ?? "",
//             };
//
//             list.Add(animeInfo);
//         }
//
//         return list;
//     }
//
//     /// <inheritdoc />
//     public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
//         string id,
//         CancellationToken cancellationToken = default
//     )
//     {
//         var response = await HttpClient.ExecuteAsync($"{BaseUrl}{id}", cancellationToken);
//
//         var document = HtmlHelper.Parse(response);
//
//         var anime = new AnimeInfo
//         {
//             Id = id,
//             Site = AnimeSites.Aniwave,
//             Title =
//                 document
//                     .DocumentNode.SelectSingleNode(".//h1[contains(@class, 'title')]")
//                     ?.InnerText
//                 ?? string.Empty,
//             Image = document
//                 .DocumentNode.SelectSingleNode(
//                     ".//div[contains(@class, 'binfo')]/div[contains(@class, 'poster')]/span/img"
//                 )
//                 .Attributes["src"]
//                 .Value,
//         };
//
//         var jpAttr = document
//             .DocumentNode.SelectSingleNode(".//h1[contains(@class, 'title')]")
//             ?.Attributes["data-jp"];
//         if (jpAttr is not null)
//             anime.OtherNames = jpAttr.Value;
//
//         anime.Summary =
//             document.DocumentNode.SelectSingleNode(".//div[@class='content']")?.InnerText?.Trim()
//             ?? "";
//
//         var genresNode = document
//             .DocumentNode.SelectNodes(".//div[contains(@class, 'meta')][1]/div")
//             ?.FirstOrDefault(x => x.InnerText?.ToLower().Contains("genres") == true)
//             ?.SelectNodes(".//span/a");
//         if (genresNode is not null)
//             anime.Genres.AddRange(genresNode.Select(x => new Genre(x.InnerText)));
//
//         var airedNode = document
//             .DocumentNode.SelectNodes(".//div[contains(@class, 'meta')][1]/div")
//             ?.FirstOrDefault(x => x.InnerText?.ToLower().Contains("aired") == true)
//             ?.SelectSingleNode(".//span");
//         if (airedNode is not null)
//             anime.Released = airedNode.InnerText.Trim();
//
//         var typeNode = document
//             .DocumentNode.SelectNodes(".//div[contains(@class, 'meta')][1]/div")
//             ?.FirstOrDefault(x => x.InnerText?.ToLower().Contains("type") == true)
//             ?.SelectSingleNode(".//span");
//         if (typeNode is not null)
//             anime.Type = typeNode.InnerText.Trim();
//
//         var statusNode = document
//             .DocumentNode.SelectNodes(".//div[contains(@class, 'meta')][1]/div")
//             ?.FirstOrDefault(x => x.InnerText?.ToLower().Contains("status") == true)
//             ?.SelectSingleNode(".//span");
//         if (statusNode is not null)
//             anime.Status = statusNode.InnerText.Trim();
//
//         return anime;
//     }
//
//     /// <inheritdoc />
//     public async ValueTask<List<Episode>> GetEpisodesAsync(
//         string id,
//         CancellationToken cancellationToken = default
//     )
//     {
//         var list = new List<Episode>();
//
//         var response = await HttpClient.ExecuteAsync($"{BaseUrl}{id}", cancellationToken);
//
//         var document = HtmlHelper.Parse(response);
//         var dataId = document
//             .DocumentNode.SelectNodes(".//div[@data-id]")!
//             .FirstOrDefault()!
//             .Attributes["data-id"]
//             .Value;
//
//         var vrf = await EncodeVrfAsync(dataId, cancellationToken);
//
//         var response2 = await HttpClient.ExecuteAsync(
//             $"{BaseUrl}/ajax/episode/list/{dataId}?vrf={vrf}",
//             new Dictionary<string, string> { ["url"] = BaseUrl + id },
//             cancellationToken
//         );
//
//         var html = JsonNode.Parse(response2)!["result"]!.ToString();
//         document = HtmlHelper.Parse(html);
//
//         var nodes = document.DocumentNode.SelectNodes(
//             ".//div[contains(@class, 'episodes')]/ul/li/a"
//         );
//         if (nodes.IsNullOrEmpty())
//             return list;
//
//         foreach (var node in nodes)
//         {
//             var possibleIds = node.Attributes["data-ids"]?.Value.Split(',')!;
//
//             list.Add(
//                 new Episode
//                 {
//                     Id = possibleIds[0], // Sub
//                     Name = node.SelectSingleNode(".//span")?.InnerText,
//                     //Link = link,
//                     Number = int.Parse(node.Attributes["data-num"]?.Value ?? "0"),
//                 }
//             );
//         }
//
//         return list;
//     }
//
//     /// <inheritdoc />
//     public async ValueTask<List<VideoServer>> GetVideoServersAsync(
//         string episodeId,
//         CancellationToken cancellationToken = default
//     )
//     {
//         var vrf = await EncodeVrfAsync(episodeId, cancellationToken);
//
//         var response = await HttpClient.ExecuteAsync(
//             $"{BaseUrl}/ajax/server/list/{episodeId}?vrf={vrf}",
//             cancellationToken
//         );
//
//         var html = JsonNode.Parse(response)!["result"]!.ToString();
//
//         var document = HtmlHelper.Parse(html);
//
//         var list = new List<VideoServer>();
//
//         var nodes = document.DocumentNode.SelectNodes(".//div[@class='type']/ul/li")?.ToList();
//         if (nodes is null)
//             return list;
//
//         var functions = Enumerable
//             .Range(0, nodes.Count)
//             .Select(i =>
//                 (Func<Task<VideoServer>>)(
//                     async () => await GetVideoServerAsync(nodes[i], cancellationToken)
//                 )
//             );
//
//         list.AddRange(await TaskHelper.Run(functions, 10));
//
//         return list;
//     }
//
//     private async ValueTask<VideoServer> GetVideoServerAsync(
//         HtmlNode node,
//         CancellationToken cancellationToken = default
//     )
//     {
//         var serverId = node.Attributes["data-link-id"].Value;
//
//         var vrf2 = await EncodeVrfAsync(serverId, cancellationToken);
//
//         var response3 = await HttpClient.ExecuteAsync(
//             $"{BaseUrl}/ajax/server/{serverId}?vrf={vrf2}",
//             cancellationToken
//         );
//
//         var encodedStreamUrl = JsonNode.Parse(response3)?["result"]?["url"]?.ToString();
//
//         var realLink = await DecodeVrfAsync(encodedStreamUrl!, cancellationToken);
//
//         return new()
//         {
//             Name = node.InnerText,
//             Embed = new(realLink) { Headers = new() { ["Referer"] = BaseUrl } },
//         };
//     }
//
//     public override IVideoExtractor? GetVideoExtractor(VideoServer server)
//     {
//         return server.Name.ToLower() switch
//         {
//             "vidstream" => new AniwaveExtractor(HttpClientFactory, "VidStream"),
//             "mycloud" => new AniwaveExtractor(HttpClientFactory, "MyCloud"),
//             "filemoon" => new FilemoonExtractor(HttpClientFactory),
//             "streamtape" => new StreamTapeExtractor(HttpClientFactory),
//             "mp4upload" => new Mp4UploadExtractor(HttpClientFactory),
//             _ => null,
//         };
//     }
//
//     /// <summary>
//     /// Encodes a string by making an http request to <see href="https://9anime.eltik.net"/>.
//     /// </summary>
//     /// <param name="query">The string to encode.</param>
//     /// <param name="cancellationToken"></param>
//     /// <returns>An encoded string.</returns>
//     public async ValueTask<string> EncodeVrfAsync(
//         string query,
//         CancellationToken cancellationToken = default
//     )
//     {
//         var response = await HttpClient.ExecuteAsync(
//             $"https://9anime.eltik.net/vrf?query={query}&apikey=chayce",
//             cancellationToken
//         );
//
//         if (string.IsNullOrWhiteSpace(response))
//             return string.Empty;
//
//         var data = JsonNode.Parse(response)!;
//
//         var vrf = data["url"]?.ToString();
//
//         if (!string.IsNullOrWhiteSpace(vrf))
//             return vrf!;
//
//         return string.Empty;
//     }
//
//     /// <summary>
//     /// Decodes a string by making an http request to <see href="https://9anime.eltik.net"/>.
//     /// </summary>
//     /// <param name="query">The string to decode.</param>
//     /// <param name="cancellationToken"></param>
//     /// <returns>A decoded string.</returns>
//     public async ValueTask<string> DecodeVrfAsync(
//         string query,
//         CancellationToken cancellationToken = default
//     )
//     {
//         var response = await HttpClient.ExecuteAsync(
//             $"https://9anime.eltik.net/decrypt?query={query}&apikey=chayce",
//             cancellationToken
//         );
//
//         if (string.IsNullOrWhiteSpace(response))
//             return string.Empty;
//
//         var data = JsonNode.Parse(response)!;
//
//         var vrf = data["url"]?.ToString();
//
//         if (!string.IsNullOrWhiteSpace(vrf))
//             return vrf!;
//
//         return string.Empty;
//     }
// }
