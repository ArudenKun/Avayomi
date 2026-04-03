using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Avalonia.Controls;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Duende.IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Avayomi.Services;

[AutoExtractInterface]
[ExposeServices(typeof(ITokenService))]
public sealed class TokenService : ITokenService, ISingletonDependency
{
    private const string RedirectUrl = "http://127.0.0.1/avayomi";
    private const string ClientId = "38430";
    private const string ResponseType = "token";

    private readonly ILogger<TokenService> _logger;
    private readonly TopLevel _topLevel;

    public TokenService(ILogger<TokenService> logger, TopLevel topLevel)
    {
        _logger = logger;
        _topLevel = topLevel;
    }

    private string StorePath { get; } =
        AvayomiCoreConsts.Paths.DataDir.Combine(
            $"{Guid.Parse("2FD0478D-4526-42A0-A0CB-67460D080671"):N}.dat"
        );

    public async Task<bool> LoginAsync()
    {
        try
        {
            var requestUrl = new RequestUrl("https://anilist.co/api/v2/oauth/authorize");
            var startUrl = requestUrl.CreateAuthorizeUrl(ClientId, ResponseType);

            var result = await WebAuthenticationBroker.AuthenticateAsync(
                _topLevel,
                new WebAuthenticatorOptions(new Uri(startUrl), new Uri(RedirectUrl))
            );
            var authorizeResponse = new AuthorizeResponse(result.CallbackUri.AbsoluteUri);
            var accessToken = authorizeResponse.AccessToken;
            if (string.IsNullOrEmpty(accessToken))
            {
                return false;
            }

            await SaveTokenAsync(accessToken);
            return true;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (!File.Exists(StorePath))
        {
            return string.Empty;
        }

        var base64 = JsonSerializer.Deserialize<string>(await File.ReadAllTextAsync(StorePath));
        return base64.IsNullOrEmpty() ? string.Empty : base64.DecodeBase64();
    }

    private async Task SaveTokenAsync(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return;

        await using var stream = File.Create(StorePath);
        var bytes = Encoding.UTF8.GetBytes(accessToken);
        var base64 = Convert.ToBase64String(bytes);
        await JsonSerializer.SerializeAsync(stream, base64);
    }
}
