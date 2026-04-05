using System;
using System.Threading;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

namespace Avayomi.Cache;

public interface IFusionCache<T>
{
    string CacheName { get; }

    // GET
    T? Get(string key);
    T? Get(string key, T? defaultValue);

    ValueTask<T?> GetAsync(string key, CancellationToken token = default);
    ValueTask<T?> GetAsync(string key, T? defaultValue, CancellationToken token = default);

    // SET
    void Set(string key, T value, FusionCacheEntryOptions? options = null);
    ValueTask SetAsync(
        string key,
        T value,
        FusionCacheEntryOptions? options = null,
        CancellationToken token = default
    );

    // GET OR SET (Factory)
    T GetOrSet(
        string key,
        Func<FusionCacheFactoryExecutionContext<T>, CancellationToken, T> factory,
        FusionCacheEntryOptions? options = null
    );

    ValueTask<T> GetOrSetAsync(
        string key,
        Func<FusionCacheFactoryExecutionContext<T>, CancellationToken, Task<T>> factory,
        FusionCacheEntryOptions? options = null,
        CancellationToken token = default
    );

    // TRY GET
    bool TryGet(string key, out T? value);

    ValueTask<(bool Success, T? Value)> TryGetAsync(string key, CancellationToken token = default);

    // REMOVE
    void Remove(string key);
    ValueTask RemoveAsync(string key, CancellationToken token = default);

    // EXPIRE
    void Expire(string key);
    ValueTask ExpireAsync(string key, CancellationToken token = default);
}
