using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Avayomi.Core;
using Avayomi.Core.Anime;
using Avayomi.Core.Extensions;
using Avayomi.Core.Providers.Anime;
using Avayomi.Core.Tasks;
using Avayomi.Core.Videos;

namespace Avayomi.Providers.Anime;

public partial class AnimePaheProvider : AnimeProviderBase, IAnimeProvider
{
    public const string BaseUrl = "https://animepahe.ru";

    public AnimePaheProvider(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public string Key => Name;

    public string Name => "AnimePahe";

    public string Language => "en";

    public bool IsDubAvailableSeparately => false;

    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    ) => await SearchAsync(query, false, cancellationToken);

    /// <inheritdoc cref="IAnimeProvider.SearchAsync" />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        bool useId,
        CancellationToken cancellationToken = default
    )
    {
        var animes = new List<IAnimeInfo>();
        var http = HttpClientFactory.CreateProviderHttpClient().BypassDdg();
        var response = await http.ExecuteAsync(
            $"{BaseUrl}/api?m=search&q={Uri.EscapeDataString(query)}",
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response))
            return animes;

        var data = JsonNode.Parse(response)?["data"];
        if (data is null)
            return animes;

        return data.AsArray()
            .Select(x =>
                (IAnimeInfo)
                    new AnimePaheInfo
                    {
                        Id = useId ? x!["id"]!.ToString() : x!["session"]!.ToString(),
                        Title = x["title"]!.ToString(),
                        Type = x["type"]!.ToString(),
                        Episodes = int.TryParse(x["episodes"]?.ToString(), out var episodes)
                            ? episodes
                            : 0,
                        Status = x["status"]!.ToString(),
                        Season = x["season"]!.ToString(),
                        Released = x["year"]?.ToString(),
                        Score = int.TryParse(x["score"]?.ToString(), out var score) ? score : 0,
                        Image = x["poster"]!.ToString(),
                        Site = AnimeSites.AnimePahe,
                    }
            )
            .ToList();
    }

    /// <inheritdoc cref="IAnimeProvider.SearchAsync"/>
    public async ValueTask<List<IAnimeInfo>> GetAiringAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var animes = new List<IAnimeInfo>();

        var http = HttpClientFactory.CreateClient().BypassDdg();

        var response = await http.ExecuteAsync(
            $"{BaseUrl}/api?m=airing&page={page}",
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response))
            return animes;

        var data = JsonNode.Parse(response)?["data"];
        if (data is null)
            return animes;

        return data.AsArray()
            .Select(x =>
                (IAnimeInfo)
                    new AnimeInfo
                    {
                        Id = x!["anime_session"]!.ToString(),
                        Title = x["anime_title"]!.ToString(),
                        Image = x["snapshot"]!.ToString(),
                        Site = AnimeSites.AnimePahe,
                    }
            )
            .ToList();
    }

    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateClient().BypassDdg();

        var response = await http.ExecuteAsync($"{BaseUrl}/anime/{animeId}", cancellationToken);

        var document = HtmlHelper.Parse(response);

        var anime = new AnimePaheInfo
        {
            Id = animeId,
            Site = AnimeSites.AnimePahe,
            Title = document
                .DocumentNode.SelectSingleNode(
                    ".//div[contains(@class, 'header-wrapper')]/header/div/h1/span"
                )
                .InnerText,
            Image = document
                .DocumentNode.SelectSingleNode(".//header/div/div/div/a/img")
                .Attributes["data-src"]
                .Value,
            Summary = document
                .DocumentNode.SelectSingleNode(".//div[contains(@class, 'anime-summary')]/div")
                .InnerText,
            Genres = document
                .DocumentNode.SelectNodes(".//div[contains(@class, 'anime-info')]/div/ul/li/a")
                .Select(el => new Genre(el.Attributes["title"].Value))
                .ToList(),
        };

        var list = document
            .DocumentNode.SelectNodes(".//div[contains(@class, 'anime-info')]/p")
            .ToList();

        var typeNode = list.Find(x =>
            x.ChildNodes.ElementAtOrDefault(0)?.InnerText.ToLower().Contains("type") == true
        );

        var otherNamesCount = typeNode is not null ? list.IndexOf(typeNode) : 0;

        anime.Type = typeNode?.SelectSingleNode(".//a").InnerText.Trim() ?? "";

        var otherNameNodes = list.Take(otherNamesCount).ToList();

        anime.OtherNames =
            otherNameNodes.FirstOrDefault()?.ChildNodes.ElementAtOrDefault(1)?.InnerText.Trim()
            ?? "";

        var releasedNode = list.Find(x =>
            x.ChildNodes.ElementAtOrDefault(1)?.InnerText.ToLower().Contains("aired") == true
        );

        anime.Released =
            releasedNode
                ?.InnerText.Split(["to"], StringSplitOptions.None)[0]
                .Trim()
                .Replace("Aired:", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("\t", "")
            ?? "";

        var statusNode = list.Find(x =>
            x.ChildNodes.ElementAtOrDefault(1)?.InnerText.ToLower().Contains("status") == true
        );

        anime.Status = statusNode?.SelectSingleNode(".//a").InnerText.Trim() ?? "";

        var seasonNode = list.Find(x =>
            x.ChildNodes.ElementAtOrDefault(0)?.InnerText.ToLower().Contains("season") == true
        );

        anime.Season = seasonNode?.SelectSingleNode(".//a").InnerText.Trim() ?? "";

        return anime;
    }

    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        var list = new List<Episode>();

        var http = HttpClientFactory.CreateClient().BypassDdg();

        var response = await http.ExecuteAsync(
            $"{BaseUrl}/api?m=release&id={animeId}&sort=episode_asc&page=1",
            cancellationToken
        );

        var data = JsonNode.Parse(response);

        Episode EpsSelector(JsonNode? el)
        {
            var link = $"{BaseUrl}/play/{animeId}/{el!["session"]}";
            var epNum = Convert.ToInt32(el["episode"]?.ToString());

            return new Episode
            {
                //Description = el["description"]!.ToString(),
                //Id = el["id"]!.ToString(),
                //Id = el["session"]!.ToString(),
                Id = link,
                Name = $"Episode {epNum}",
                Number = epNum,
                Image = el["snapshot"]?.ToString(),
                Description = el["title"]?.ToString(),
                Link = link,
                Duration = (float)TimeSpan.Parse(el["duration"]!.ToString()).TotalMilliseconds,
            };
        }

        list.AddRange(data!["data"]!.AsArray().Select(EpsSelector));

        var lastPage = Convert.ToInt32(data["last_page"]?.ToString());

        if (lastPage < 2)
            return list;

        // Start at index of 2 since we've already gotten the first page above.
        var functions = Enumerable
            .Range(2, lastPage - 1)
            .Select(i =>
                (Func<Task<string>>)(
                    async () =>
                        await http.ExecuteAsync(
                            $"{BaseUrl}/api?m=release&id={animeId}&sort=episode_asc&page={i}",
                            cancellationToken
                        )
                )
            );

        var results = await TaskHelper.Run(functions, 20);

        list.AddRange(
            results.SelectMany(x => JsonNode.Parse(x)!["data"]!.AsArray().Select(EpsSelector))
        );

        return list;
    }

    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        var http = HttpClientFactory.CreateProviderHttpClient().BypassDdg();

        var response = await http.ExecuteAsync(
            //$"{BaseUrl}/api?m=links&id={episodeId}",
            episodeId,
            new Dictionary<string, string> { { "Referer", BaseUrl } },
            cancellationToken
        );

        var document = HtmlHelper.Parse(response);

        return document
            .GetElementbyId("pickDownload")
            .SelectNodes(".//a")
            .Select(el =>
            {
                //var match = _videoServerRegex.Match(el.InnerText);
                //var matches = _videoServerRegex.Matches(el.InnerText).OfType<Match>().ToList();
                var match = VideoServerRegex.Match(el.InnerText);
                var groups = match.Groups.OfType<Group>().ToArray();

                var subgroup = groups.ElementAtOrDefault(1)?.Value;
                var quality = groups.ElementAtOrDefault(2)?.Value;
                var audio = groups.ElementAtOrDefault(4)?.Value;

                var audioName = !string.IsNullOrWhiteSpace(audio) ? $"{audio} " : "";

                return new VideoServer
                {
                    Name = $"{subgroup} {audioName}- {quality}p",
                    Embed = new FileUrl
                    {
                        Url = el.Attributes["href"].Value,
                        Headers = new Dictionary<string, string> { { "Referer", BaseUrl } },
                    },
                };
            })
            .ToList();
    }

    [GeneratedRegex(@"(.+) · (.+)p \((.+)MB\) ?(.*)")]
    private static partial Regex VideoServerRegex { get; }
}
