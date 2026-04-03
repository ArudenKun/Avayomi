using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Web;
using Avayomi.Core;
using Avayomi.Core.AniList;
using Avayomi.Core.Anime;
using Avayomi.Core.GraphQL;
using Avayomi.Core.Providers.Anime;
using Avayomi.Core.Videos;
using Volo.Abp.Data;

namespace Avayomi.Providers.Anime;

public class AllManga : AnimeBaseProvider, IAnimeProvider
{
    private const string AllAnimeBase = "allanime.day";
    private const string AllAnimeApi = "https://api.allanime.day";
    private const string AllAnimeReferrer = "https://allmanga.to";

    public AllManga(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public string Name => "AllManga";
    public string Language => "en";
    public string Key => Name;
    public bool IsDubAvailableSeparately => false;

    protected override HttpClient HttpClient
    {
        get
        {
            var client = HttpClientFactory.CreateClient("AllManga");
            client.DefaultRequestHeaders.Add("User-Agent", HttpHelper.ChromeUserAgent());
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Referer", AllAnimeReferrer);
            return client;
        }
    }

    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        List<GqlParameter> filters =
        [
            new(
                "search",
                new List<GqlParameter>
                {
                    new("allowAdult", false),
                    new("allowUnknown", false),
                    new("query", query),
                }
            ),
            new("limit", 40),
            new("page", 1),
            new("translationType", TranslationType.Sub),
            new("countryOrigin", CountryOrigin.All),
        ];
        var selections = new GqlSelection("shows")
        {
            Parameters = filters,
            Selections =
            [
                new GqlSelection("edges", GqlParser.ParseToSelections<AllMangaAnimeInfo>()),
            ],
        };

        var response = await PostRequestAsync(selections);
        var results = GqlParser.ParseFromJson<AllMangaAnimeInfo[]>(response["shows"]!["edges"]!);
        return results
                ?.OrderByDescending(x => x.Score)
                .Select(
                    IAnimeInfo (x) =>
                        new AnimeInfo
                        {
                            Image = x.Thumbnail,
                            Status = x.Status,
                            Summary = x.Description,
                            Id = x.Id,
                            Title = x.Name,
                            Episodes = int.Parse(x.EpisodeCount),
                            OtherNames = x.NativeName,
                            Type = "Anime",
                        }
                )
                .ToList()
            ?? [];
    }

    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string animeIdOrUrl,
        CancellationToken cancellationToken = default
    )
    {
        string showId = animeIdOrUrl.Contains("/") ? animeIdOrUrl.Split('/').Last() : animeIdOrUrl;

        const string detailsGql =
            @"query($showId: String!) { 
                show(_id: $showId) { 
                    _id
                    name
                    malId
                    aniListId
                    description
                    availableEpisodesDetail
                    thumbnail
                    __typename
                }
            }";

        var variables = new { showId = showId };

        string requestUrl =
            $"{AllAnimeApi}/api?variables={HttpUtility.UrlEncode(JsonSerializer.Serialize(variables))}&query={HttpUtility.UrlEncode(detailsGql)}";

        string response = await HttpClient.GetStringAsync(requestUrl);
        JsonDocument jsonDoc = JsonDocument.Parse(response);

        if (
            jsonDoc.RootElement.TryGetProperty("data", out JsonElement data)
            && data.TryGetProperty("show", out JsonElement show)
        )
        {
            AnimeInfo details = new()
            {
                Id = show.GetProperty("_id").GetString() ?? string.Empty,
                Title = show.GetProperty("name").GetString() ?? string.Empty,
            };

            if (
                show.TryGetProperty("malId", out JsonElement malIdProp)
                && malIdProp.ValueKind == JsonValueKind.Number
            )
            {
                details.SetProperty("MalId", malIdProp.GetInt32());
            }

            if (
                show.TryGetProperty("aniListId", out JsonElement aniListIdProp)
                && aniListIdProp.ValueKind == JsonValueKind.Number
            )
            {
                details.SetProperty("AniListId", aniListIdProp.GetInt32());
            }

            if (show.TryGetProperty("description", out JsonElement descProp))
            {
                details.Summary = descProp.GetString();
            }

            if (show.TryGetProperty("thumbnail", out JsonElement thumbProp))
            {
                details.Image = thumbProp.GetString();
            }

            return details;
        }

        return new AnimeInfo();
    }

    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string animeIdOrUrl,
        CancellationToken cancellationToken = default
    )
    {
        string showId = animeIdOrUrl.Contains("/") ? animeIdOrUrl.Split('/').Last() : animeIdOrUrl;

        const string episodesGql =
            @"query($showId: String!) { 
                show(_id: $showId) { 
                    _id 
                    availableEpisodesDetail 
                }
            }";

        var variables = new { showId = showId };

        string requestUrl =
            $"{AllAnimeApi}/api?variables={HttpUtility.UrlEncode(JsonSerializer.Serialize(variables))}&query={HttpUtility.UrlEncode(episodesGql)}";

        string response = await HttpClient.GetStringAsync(requestUrl);
        JsonDocument jsonDoc = JsonDocument.Parse(response);

        List<Episode> episodes = new();

        if (
            jsonDoc.RootElement.TryGetProperty("data", out JsonElement data)
            && data.TryGetProperty("show", out JsonElement show)
            && show.TryGetProperty("availableEpisodesDetail", out JsonElement episodesDetail)
            && episodesDetail.TryGetProperty("sub", out JsonElement subEpisodes)
        )
        {
            foreach (JsonElement ep in subEpisodes.EnumerateArray())
            {
                string? epString = ep.GetString();
                if (float.TryParse(epString, out float epNum))
                {
                    episodes.Add(
                        new Episode
                        {
                            Id = $"{showId}-{epString}",
                            Number = (int)epNum,
                            Link = $"{AllAnimeReferrer}/anime/{showId}/episodes/sub/{epString}",
                            // ShowId = showId,
                            // EpisodeString = epString,
                            // TotalEpisodes = subEpisodes.GetArrayLength(),
                        }
                    );
                }
            }
        }

        return episodes.OrderBy(e => e.Number).ToList();
    }

    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        // Expected format: https://allmanga.to/anime/{showId}/episodes/sub/{episodeString}
        string[] parts = episodeId.Split('/');
        string showId = parts[^4];
        string episodeString = parts[^1];

        const string episodeGql =
            @"query($showId: String!, $translationType: VaildTranslationTypeEnumType!, $episodeString: String!) { 
                episode(showId: $showId translationType: $translationType episodeString: $episodeString) { 
                    episodeString 
                    sourceUrls 
                }
            }";

        var variables = new
        {
            showId = showId,
            translationType = "sub",
            episodeString = episodeString,
        };

        string requestUrl =
            $"{AllAnimeApi}/api?variables={HttpUtility.UrlEncode(JsonSerializer.Serialize(variables))}&query={HttpUtility.UrlEncode(episodeGql)}";

        string response = await HttpClient.GetStringAsync(requestUrl);

        JsonDocument jsonDoc = JsonDocument.Parse(response);

        var videoServers = new List<VideoServer>();
        if (
            jsonDoc.RootElement.TryGetProperty("data", out JsonElement data)
            && data.TryGetProperty("episode", out JsonElement episode)
            && episode.TryGetProperty("sourceUrls", out JsonElement sourceUrls)
        )
        {
            foreach (JsonElement source in sourceUrls.EnumerateArray())
            {
                if (source.TryGetProperty("sourceUrl", out JsonElement sourceUrl))
                {
                    string? encodedUrl = sourceUrl.GetString();
                    if (!string.IsNullOrEmpty(encodedUrl))
                    {
                        string decodedUrl = DecodeSourceUrl(encodedUrl);
                        var videoLink = await GetLinksFromSource(decodedUrl);
                        if (videoLink is not null)
                            videoServers.Add(videoLink);
                        ;
                    }
                }
            }

            return videoServers;
        }

        throw new Exception("No valid video sources found");
    }

    private string DecodeSourceUrl(string encoded)
    {
        if (string.IsNullOrEmpty(encoded) || !encoded.StartsWith("--"))
            return encoded;

        encoded = encoded.Substring(2);
        StringBuilder decoded = new();

        for (int i = 0; i < encoded.Length; i += 2)
        {
            if (i + 1 < encoded.Length)
            {
                string hex = encoded.Substring(i, 2);
                string chr = DecodeHexChar(hex);
                if (!string.IsNullOrEmpty(chr))
                    decoded.Append(chr);
            }
        }

        return decoded.ToString().Replace("/clock", "/clock.json");
    }

    private string DecodeHexChar(string hex)
    {
        return hex switch
        {
            "79" => "A",
            "7a" => "B",
            "7b" => "C",
            "7c" => "D",
            "7d" => "E",
            "7e" => "F",
            "7f" => "G",
            "70" => "H",
            "71" => "I",
            "72" => "J",
            "73" => "K",
            "74" => "L",
            "75" => "M",
            "76" => "N",
            "77" => "O",
            "68" => "P",
            "69" => "Q",
            "6a" => "R",
            "6b" => "S",
            "6c" => "T",
            "6d" => "U",
            "6e" => "V",
            "6f" => "W",
            "60" => "X",
            "61" => "Y",
            "62" => "Z",
            "59" => "a",
            "5a" => "b",
            "5b" => "c",
            "5c" => "d",
            "5d" => "e",
            "5e" => "f",
            "5f" => "g",
            "50" => "h",
            "51" => "i",
            "52" => "j",
            "53" => "k",
            "54" => "l",
            "55" => "m",
            "56" => "n",
            "57" => "o",
            "48" => "p",
            "49" => "q",
            "4a" => "r",
            "4b" => "s",
            "4c" => "t",
            "4d" => "u",
            "4e" => "v",
            "4f" => "w",
            "40" => "x",
            "41" => "y",
            "42" => "z",
            "08" => "0",
            "09" => "1",
            "0a" => "2",
            "0b" => "3",
            "0c" => "4",
            "0d" => "5",
            "0e" => "6",
            "0f" => "7",
            "00" => "8",
            "01" => "9",
            "15" => "-",
            "16" => ".",
            "67" => "_",
            "46" => "~",
            "02" => ":",
            "17" => "/",
            "07" => "?",
            "1b" => "#",
            "63" => "[",
            "65" => "]",
            "78" => "@",
            "19" => "!",
            "1c" => "$",
            "1e" => "&",
            "10" => "(",
            "11" => ")",
            "12" => "*",
            "13" => "+",
            "14" => ",",
            "03" => ";",
            "05" => "=",
            "1d" => "%",
            _ => "",
        };
    }

    private async Task<VideoServer?> GetLinksFromSource(string sourceUrl)
    {
        try
        {
            string response = await HttpClient.GetStringAsync($"https://{AllAnimeBase}{sourceUrl}");
            JsonDocument jsonDoc = JsonDocument.Parse(response);

            if (jsonDoc.RootElement.TryGetProperty("links", out JsonElement links))
            {
                foreach (JsonElement link in links.EnumerateArray())
                {
                    if (
                        link.TryGetProperty("link", out JsonElement linkProp)
                        && link.TryGetProperty("resolutionStr", out JsonElement _)
                    )
                    {
                        string? url = linkProp.GetString();
                        if (!string.IsNullOrEmpty(url))
                        {
                            return new VideoServer(url, new FileUrl { Url = url });
                        }
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<JsonNode> PostRequestAsync(GqlSelection selection, bool isMutation = false)
    {
        // Build selection
        var bodyJson = new JsonObject
        {
            ["query"] = (isMutation ? "mutation" : string.Empty) + selection,
        };
        var bodyText = bodyJson["query"]!.GetValue<string>();
        var body = new StringContent(bodyJson.ToJsonString(), Encoding.UTF8, "application/json");

        // Send request
        var response = await HttpClient.PostAsync($"{AllAnimeApi}/api", body);

        // Parse response
        var responseText = await response.Content.ReadAsStringAsync();
        var responseJson = JsonNode.Parse(responseText);

        Console.WriteLine(responseText);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage =
                responseJson?["errors"]?[0]?["message"]?.GetValue<string>()
                ?? "Unknown GraphQL Error";
            throw new AniListException(errorMessage, bodyText, responseText, response.StatusCode);
        }

        return responseJson?["data"]!;
    }

    private async Task<JsonNode> GetSingleDataAsync(params GqlSelection[] path)
    {
        // Build path to selection
        var selection = path[^1];
        for (var index = path.Length - 2; index >= 0; index--)
        {
            var newSelection = path[index];
            newSelection.Selections ??= new List<GqlSelection>();
            newSelection.Selections.Add(selection);
            selection = newSelection;
        }

        // Send request
        var token = await PostRequestAsync(selection);

        // Get value from path
        token = path.Aggregate(token, (current, item) => current[item.Name]!);

        return token;
    }
}

public enum TranslationType
{
    [EnumMember(Value = "raw")]
    Raw,

    [EnumMember(Value = "sub")]
    Sub,

    [EnumMember(Value = "dub")]
    Dub,
}

public enum CountryOrigin
{
    [EnumMember(Value = "ALL")]
    All,

    [EnumMember(Value = "CN")]
    China,

    [EnumMember(Value = "JP")]
    Japan,

    [EnumMember(Value = "KR")]
    Korea,

    [EnumMember(Value = "OTHER")]
    Other,
}

public class AiredEnd
{
    [JsonPropertyName("year")]
    public int Year { get; private set; }

    [JsonPropertyName("month")]
    public int Month { get; private set; }

    [JsonPropertyName("date")]
    public int Date { get; private set; }
}

public class AiredStart
{
    [JsonPropertyName("year")]
    public int Year { get; private set; }

    [JsonPropertyName("month")]
    public int Month { get; private set; }

    [JsonPropertyName("date")]
    public int Date { get; private set; }
}

public class AvailableEpisodes
{
    [JsonPropertyName("sub")]
    public int Sub { get; private set; }

    [JsonPropertyName("dub")]
    public int Dub { get; private set; }

    [JsonPropertyName("raw")]
    public int Raw { get; private set; }
}

public class Character
{
    [GqlSelection("role")]
    public string Role { get; private set; }

    [GqlSelection("name")]
    public Name Name { get; private set; }

    [GqlSelection("image")]
    public Image Image { get; private set; }

    [GqlSelection("aniListId")]
    public int AniListId { get; private set; }

    [GqlSelection("voiceActors")]
    public IReadOnlyList<VoiceActor> VoiceActors { get; private set; }
}

public class Image
{
    [GqlSelection("large")]
    public string Large { get; private set; }

    [GqlSelection("medium")]
    public string Medium { get; private set; }
}

public class Name
{
    [GqlSelection("full")]
    public string Full { get; private set; }

    [GqlSelection("native")]
    public string Native { get; private set; }
}

public class AllMangaAnimeInfo
{
    [GqlSelection("_id")]
    public string Id { get; private set; }

    [GqlSelection("malId")]
    public string MalId { get; private set; }

    [GqlSelection("aniListId")]
    public string AniListId { get; private set; }

    [GqlSelection("description")]
    public string Description { get; private set; }

    [GqlSelection("name")]
    public string Name { get; private set; }

    [GqlSelection("nativeName")]
    public string NativeName { get; private set; }

    [GqlSelection("altNames")]
    public IReadOnlyList<string> AltNames { get; private set; }

    [GqlSelection("englishName")]
    public string EnglishName { get; private set; }

    [GqlSelection("trustedAltNames")]
    public IReadOnlyList<string> TrustedAltNames { get; private set; }

    [GqlSelection("genres")]
    public IReadOnlyList<string> Genres { get; private set; }

    [GqlSelection("availableEpisodes")]
    public AvailableEpisodes AvailableEpisodes { get; private set; }

    [GqlSelection("score")]
    public double? Score { get; private set; }

    [GqlSelection("averageScore")]
    public int? AverageScore { get; private set; }

    [GqlSelection("banner")]
    public string Banner { get; private set; }

    [GqlSelection("thumbnail")]
    public string Thumbnail { get; private set; }

    [GqlSelection("episodeCount")]
    public string EpisodeCount { get; private set; }

    [GqlSelection("characters")]
    public IReadOnlyList<Character> Characters { get; private set; }

    [GqlSelection("characterCount")]
    public string CharacterCount { get; private set; }

    [GqlSelection("status")]
    public string Status { get; private set; }

    [GqlSelection("airedStart")]
    public AiredStart AiredStart { get; private set; }

    [GqlSelection("airedEnd")]
    public AiredEnd AiredEnd { get; private set; }
}

public class VoiceActor
{
    [GqlSelection("language")]
    public string Language { get; private set; }

    [GqlSelection("aniListId")]
    public int AniListId { get; private set; }
}
