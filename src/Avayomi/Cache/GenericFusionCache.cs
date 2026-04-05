using System;
using System.Threading;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;
using IFusionCache = ZiggyCreatures.Caching.Fusion.IFusionCache;

namespace Avayomi.Cache;

using System;
using System.Threading;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

public sealed class FusionCacheWrapper<T> : IFusionCache<T>
{
    private readonly IFusionCache _cache;

    public FusionCacheWrapper(IFusionCache cache)
    {
        _cache = cache;
    }

    public string CacheName => _cache.CacheName;

    // GET
    public T? Get(string key) => _cache.GetOrDefault<T>(key);

    public T? Get(string key, T? defaultValue) => _cache.GetOrDefault<T>(key, defaultValue);

    public async ValueTask<T?> GetAsync(string key, CancellationToken token = default) =>
        await _cache.GetOrDefaultAsync<T>(key, token: token);

    public async ValueTask<T?> GetAsync(
        string key,
        T? defaultValue,
        CancellationToken token = default
    ) => await _cache.GetOrDefaultAsync<T>(key, defaultValue, token: token);

    // SET
    public void Set(string key, T value, FusionCacheEntryOptions? options = null) =>
        _cache.Set(key, value, options);

    public ValueTask SetAsync(
        string key,
        T value,
        FusionCacheEntryOptions? options = null,
        CancellationToken token = default
    ) => _cache.SetAsync(key, value, options, token);

    // GET OR SET
    public T GetOrSet(
        string key,
        Func<FusionCacheFactoryExecutionContext<T>, CancellationToken, T> factory,
        FusionCacheEntryOptions? options = null
    ) => _cache.GetOrSet<T>(key, factory, options);

    public ValueTask<T> GetOrSetAsync(
        string key,
        Func<FusionCacheFactoryExecutionContext<T>, CancellationToken, Task<T>> factory,
        FusionCacheEntryOptions? options = null,
        CancellationToken token = default
    ) => _cache.GetOrSetAsync(key, factory, options, token);

    // TRY GET
    public bool TryGet(string key, out T? value)
    {
        var maybeValue = _cache.TryGet<T>(key);
        value = maybeValue.GetValueOrDefault();
        return maybeValue.HasValue;
    }

    public async ValueTask<(bool Success, T? Value)> TryGetAsync(
        string key,
        CancellationToken token = default
    )
    {
        var maybeValue = await _cache.TryGetAsync<T>(key, token: token);
        return (maybeValue.HasValue, maybeValue.GetValueOrDefault());
    }

    // REMOVE
    public void Remove(string key) => _cache.Remove(key);

    public ValueTask RemoveAsync(string key, CancellationToken token = default) =>
        _cache.RemoveAsync(key, token: token);

    // EXPIRE
    public void Expire(string key) => _cache.Expire(key);

    public ValueTask ExpireAsync(string key, CancellationToken token = default) =>
        _cache.ExpireAsync(key, token: token);
}
