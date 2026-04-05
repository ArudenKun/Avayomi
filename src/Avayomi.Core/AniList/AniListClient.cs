using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using AutoInterfaceAttributes;
using Avayomi.Core.GraphQL;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Avayomi.Core.AniList;

[PublicAPI]
[AutoInterface]
internal partial class AniListClient : IAniListClient
{
    private readonly AniListClientOptions _options;
    private readonly HttpClient _httpClient;

    public event EventHandler<AniListRateLimitEventArgs>? RateChanged;

    public AniListClient(IOptions<AniListClientOptions> options, HttpClient? httpClient = null)
    {
        _options = options.Value;
        _httpClient = httpClient ?? new HttpClient { BaseAddress = new Uri(options.Value.Url) };
    }

    public bool IsAuthenticated { get; private set; }

    public async Task<bool> TryAuthenticateAsync(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );
        try
        {
            _ = await GetAuthenticatedUserAsync();
            IsAuthenticated = true;
        }
        catch (AniListException aniException)
        {
            if (aniException.StatusCode != HttpStatusCode.Unauthorized)
                throw;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            IsAuthenticated = false;
        }

        return IsAuthenticated;
    }

    private async Task<JsonNode> PostRequestAsync(
        GqlSelection selection,
        bool isMutation = false,
        CancellationToken cancellationToken = default
    )
    {
        // Build selection
        var bodyJson = new JsonObject
        {
            ["query"] = (isMutation ? "mutation" : string.Empty) + selection,
        };
        var bodyText = bodyJson["query"]!.GetValue<string>();
        var body = new StringContent(bodyJson.ToJsonString(), Encoding.UTF8, "application/json");

        // Send request
        var response = await _httpClient.PostAsync(_options.Url, body, cancellationToken);

        // Parse response
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseJson = JsonNode.Parse(responseText);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage =
                responseJson?["errors"]?[0]?["message"]?.GetValue<string>()
                ?? "Unknown GraphQL Error";
            throw new AniListException(errorMessage, bodyText, responseText, response.StatusCode);
        }

        // Check rate limit
        response.Headers.TryGetValues("Retry-After", out var retryAfterValues);
        response.Headers.TryGetValues("X-RateLimit-Limit", out var rateLimitValues);
        response.Headers.TryGetValues("X-RateLimit-Remaining", out var rateRemainingValues);
        response.Headers.TryGetValues("X-RateLimit-Reset", out var rateResetValues);

        var retryAfterString = retryAfterValues?.FirstOrDefault();
        var rateLimitString = rateLimitValues?.FirstOrDefault();
        var rateRemainingString = rateRemainingValues?.FirstOrDefault();
        var rateResetString = rateResetValues?.FirstOrDefault();

        var retryAfterValidated = int.TryParse(retryAfterString, out var retryAfter);
        var rateLimitValidated = int.TryParse(rateLimitString, out var rateLimit);
        var rateRemainingValidated = int.TryParse(rateRemainingString, out var rateRemaining);
        var rateResetValidated = int.TryParse(rateResetString, out var rateReset);

        if (
            retryAfterValidated
            && rateLimitValidated
            && rateRemainingValidated
            && rateResetValidated
        )
            RateChanged?.Invoke(
                this,
                new AniListRateLimitEventArgs(rateLimit, rateRemaining, retryAfter, rateReset)
            );
        else if (rateLimitValidated && rateRemainingValidated)
            RateChanged?.Invoke(this, new AniListRateLimitEventArgs(rateLimit, rateRemaining));

        return responseJson?["data"]!;
    }

    private async Task<JsonNode> GetSingleDataAsync(
        GqlSelection[] path,
        CancellationToken cancellationToken
    )
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
        var token = await PostRequestAsync(selection, cancellationToken: cancellationToken);

        // Get value from path
        token = path.Aggregate(token, (current, item) => current[item.Name]!);

        return token;
    }
}
