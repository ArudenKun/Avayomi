using System.Threading;
using System.Threading.Tasks;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Avayomi.Core.AniList;
using Avayomi.Core.AniList.Models.User;
using Humanizer;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace Avayomi.Services;

[AutoExtractInterface]
[ExposeServices(typeof(IAniListService))]
public class AniListService : IAniListService, ISingletonDependency
{
    private readonly ILogger<AniListService> _logger;
    private readonly IAniListClient _aniListClient;
    private readonly ITokenService _tokenService;
    private readonly IFusionCache _fusionCache;

    private const string TagPrefix = "AniList.";
    private const string UserTag = TagPrefix + "User";

    public AniListService(
        ILogger<AniListService> logger,
        IAniListClient aniListClient,
        ITokenService tokenService,
        IFusionCache fusionCache
    )
    {
        _logger = logger;
        _aniListClient = aniListClient;
        _tokenService = tokenService;
        _fusionCache = fusionCache;
    }

    public bool IsAuthenticated { get; private set; }

    public async Task CheckAuthenticationCacheAsync()
    {
        var accessToken = await _tokenService.GetAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            IsAuthenticated = false;
            return;
        }

        IsAuthenticated = await _aniListClient.TryAuthenticateAsync(accessToken);
        if (!IsAuthenticated)
        {
            _logger.LogWarning("Failed to authenticate");
        }
        else
        {
            var user = await _aniListClient.GetAuthenticatedUserAsync();
            _logger.LogInformation("User: {Name}", user.Name);
            _logger.LogInformation("Using Cached AniList Login");
        }
    }

    public async Task AuthenticateAsync(string accessToken)
    {
        IsAuthenticated = await _aniListClient.TryAuthenticateAsync(accessToken);
        if (IsAuthenticated)
        {
            await _tokenService.SaveAsync(accessToken);
        }
    }

    public async Task LogoutAsync()
    {
        await _tokenService.ClearAsync();
        await _fusionCache.RemoveAsync("Authenticated-AniListUser");
        IsAuthenticated = false;
    }

    public async ValueTask<User> GetAuthenticatedUserAsync(
        CancellationToken cancellationToken = default
    )
    {
        var user = await _fusionCache.GetOrSetAsync(
            "Authenticated-AniListUser",
            async ct => await _aniListClient.GetAuthenticatedUserAsync(ct),
            options => options.SetDuration(30.Minutes()),
            [UserTag],
            cancellationToken
        );
        return user;
    }

    public async ValueTask<User> GetUserAsync(
        int userId,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _fusionCache.GetOrSetAsync(
            $"AniListUser-{userId}",
            async ct => await _aniListClient.GetUserAsync(userId, ct),
            _ => { },
            [UserTag, $"{userId}"],
            cancellationToken
        );
        return user;
    }
}
