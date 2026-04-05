using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Avayomi.Core.Animes;
using Avayomi.Core.Providers.Anime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace Avayomi.Services;

[AutoExtractInterface]
[ExposeServices(typeof(IAnimeService))]
public class AnimeService : IAnimeService, ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ReadOnlyDictionary<string, Type> _animeProviders;
    private readonly IFusionCache _fusionCache;
    private readonly ILogger<AnimeService> _logger;

    private IAnimeProvider _currentProvider;

    public AnimeService(
        IServiceProvider serviceProvider,
        IFusionCache fusionCache,
        ILogger<AnimeService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _fusionCache = fusionCache;
        _logger = logger;

        _animeProviders = serviceProvider
            .GetRequiredService<IEnumerable<IAnimeProvider>>()
            .ToDictionary(x => x.Name, x => x.GetType())
            .AsReadOnly();

        var animeProvider = _animeProviders.First();
        _currentProvider = (IAnimeProvider)serviceProvider.GetRequiredService(animeProvider.Value);
    }

    public string CurrentProvider => _currentProvider.Name;

    public void SetProvider(string provider)
    {
        if (CurrentProvider.Equals(provider, StringComparison.InvariantCultureIgnoreCase))
            return;
        _logger.LogInformation("Provider {provider}", provider);
        _currentProvider = (IAnimeProvider)
            _serviceProvider.GetRequiredService(_animeProviders[provider]);
    }

    public async ValueTask<IReadOnlyList<AnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    ) =>
        await _fusionCache.GetOrSetAsync(
            $"Search-{query}-{CurrentProvider}",
            async ct => await _currentProvider.SearchAsync(query, ct),
            _ => { },
            [.. GetProviderTags(CurrentProvider)],
            cancellationToken
        );

    public IReadOnlyList<string> GetProviders() => _animeProviders.Keys.ToList();

    public ValueTask<IReadOnlyList<string>> GetProvidersAsync() =>
        ValueTask.FromResult(GetProviders());

    private static string[] GetProviderTags(params string[] tags)
    {
        return ["Provider", .. tags];
    }
}
