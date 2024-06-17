using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using AutoInterfaceAttributes;
using Avayomi.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nito.AsyncEx;

namespace Avayomi.Data.Caching;

[AutoInterface(Inheritance = [typeof(IDistributedCache), typeof(IAsyncDisposable)])]
public sealed partial class FileCache : IFileCache
{
    private bool _disposed;
    private readonly FileCacheOptions _options;
    private readonly ILogger<FileCache> _logger;
    private readonly string _manifestPath;

    private readonly CancellationTokenSource _removingExpiredCancellationTokenSource;
    private readonly CancellationTokenSource _manifestSavingCancellationTokenSource;
    private readonly Task _backgroundRemovingExpiredTask;
    private readonly Task _backgroundManifestSavingTask;

    private readonly SemaphoreSlim _manifestLock = new(1, 1);

    private ConcurrentDictionary<string, ManifestEntry>? _cacheManifest;
    private readonly ConcurrentDictionary<string, AsyncReaderWriterLock> _fileLock;

    private readonly JsonTypeInfo _jsonTypeInfo;

    public FileCache(
        FileCacheOptions options,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<FileCache>? logger
    )
    {
        _options = options;
        _jsonTypeInfo = jsonSerializerOptions.GetTypeInfo(typeof(ConcurrentDictionary<string, ManifestEntry>));
        _logger = logger ?? NullLogger<FileCache>.Instance;
        _manifestPath = Path.Combine(options.DirectoryPath, "manifest.json");
        _fileLock = new ConcurrentDictionary<string, AsyncReaderWriterLock>(StringComparer.Ordinal);

        _removingExpiredCancellationTokenSource = new CancellationTokenSource();
        _manifestSavingCancellationTokenSource = new CancellationTokenSource();
        _backgroundRemovingExpiredTask = BackgroundRemovingExpired();
        _backgroundManifestSavingTask = BackgroundManifestSaving();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await _removingExpiredCancellationTokenSource.CancelAsync();
        if (!_backgroundRemovingExpiredTask.IsFaulted)
        {
            await _backgroundRemovingExpiredTask;
        }

        await _manifestSavingCancellationTokenSource.CancelAsync();
        if (!_backgroundManifestSavingTask.IsFaulted)
        {
            await _backgroundManifestSavingTask;
        }

        await SaveManifestAsync();
        _manifestLock.Dispose();
        _disposed = true;
    }


    public byte[]? Get(string key)
    {
        TryLoadManifest();

        if (_cacheManifest is null)
            return default;

        if (!_cacheManifest.TryGetValue(key, out var manifestEntry))
            return default;

        var lockObj = _fileLock.GetOrAdd(
            manifestEntry.FileName,
            static _ => new AsyncReaderWriterLock()
        );

        using (lockObj.ReaderLock())
        {
            //By the time we have the lock, confirm we still have a cache
            if (!_cacheManifest.ContainsKey(key))
                return default;

            var path = Path.Combine(_options.DirectoryPath, manifestEntry.FileName);
            if (File.Exists(path))
            {
                using var stream = File.OpenRead(path);
                using var memStream = new MemoryStream((int)stream.Length);
                stream.CopyTo(memStream);
                memStream.Seek(0, SeekOrigin.Begin);
                return memStream.ToArray();
            }

            //Mismatch between manifest and file system - remove from manifest
            _cacheManifest.TryRemove(key, out _);
            _fileLock.TryRemove(key, out _);
        }

        return default;
    }


    public async Task<byte[]?> GetAsync(string key, CancellationToken token = new())
    {
        await TryLoadManifestAsync();

        if (_cacheManifest is null)
            return default;

        if (!_cacheManifest.TryGetValue(key, out var manifestEntry))
            return default;

        var lockObj = _fileLock.GetOrAdd(
            manifestEntry.FileName,
            static _ => new AsyncReaderWriterLock()
        );

        using (await lockObj.ReaderLockAsync(token))
        {
            //By the time we have the lock, confirm we still have a cache
            if (!_cacheManifest.ContainsKey(key))
                return default;

            var path = Path.Combine(_options.DirectoryPath, manifestEntry.FileName);
            if (File.Exists(path))
            {
                await using var stream = File.OpenRead(path);
                using var memStream = new MemoryStream((int)stream.Length);
                await stream.CopyToAsync(memStream, token);
                memStream.Seek(0, SeekOrigin.Begin);
                return memStream.ToArray();
            }

            //Mismatch between manifest and file system - remove from manifest
            _cacheManifest.TryRemove(key, out _);
            _fileLock.TryRemove(key, out _);
        }

        return default;
    }


    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        TryLoadManifest();

        if (_cacheManifest is null)
            return;

        DateTimeOffset? expiry = null;
        TimeSpan? renewal = null;

        if (options.AbsoluteExpiration.HasValue)
        {
            expiry = options.AbsoluteExpiration.Value.ToUniversalTime();
        }
        else if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            expiry = DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
        }

        if (options.SlidingExpiration.HasValue)
        {
            renewal = options.SlidingExpiration.Value;
            expiry = (expiry ?? DateTimeOffset.UtcNow) + renewal;
        }

        //Update the manifest entry with the new expiry
        if (_cacheManifest.TryGetValue(key, out var manifestEntry))
        {
            manifestEntry = manifestEntry with { Expiry = expiry, Renewal = renewal };
        }
        else
        {
            manifestEntry = new ManifestEntry(MD5HashHelper.ComputeHash(key), expiry, renewal);
        }

        _cacheManifest[key] = manifestEntry;

        var lockObj = _fileLock.GetOrAdd(
            manifestEntry.FileName,
            static _ => new AsyncReaderWriterLock()
        );

        using (lockObj.WriterLock())
        {
            var path = Path.Combine(_options.DirectoryPath, manifestEntry.FileName);
            using var fileStream = File.OpenWrite(path);
            using var memStream = new MemoryStream(value);
            memStream.CopyTo(fileStream);
        }
    }


    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = new()
    )
    {
        await TryLoadManifestAsync();

        if (_cacheManifest is null)
            return;

        DateTimeOffset? expiry = null;
        TimeSpan? renewal = null;

        if (options.AbsoluteExpiration.HasValue)
        {
            expiry = options.AbsoluteExpiration.Value.ToUniversalTime();
        }
        else if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            expiry = DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
        }

        if (options.SlidingExpiration.HasValue)
        {
            renewal = options.SlidingExpiration.Value;
            expiry = (expiry ?? DateTimeOffset.UtcNow) + renewal;
        }

        //Update the manifest entry with the new expiry
        if (_cacheManifest.TryGetValue(key, out var manifestEntry))
        {
            manifestEntry = manifestEntry with { Expiry = expiry, Renewal = renewal };
        }
        else
        {
            manifestEntry = new ManifestEntry(MD5HashHelper.ComputeHash(key), expiry, renewal);
        }

        _cacheManifest[key] = manifestEntry;

        var lockObj = _fileLock.GetOrAdd(
            manifestEntry.FileName,
            static _ => new AsyncReaderWriterLock()
        );

        using (await lockObj.WriterLockAsync(token))
        {
            var path = Path.Combine(_options.DirectoryPath, manifestEntry.FileName);
            await using var fileStream = File.OpenWrite(path);
            using var memStream = new MemoryStream(value);
            await memStream.CopyToAsync(fileStream, token);
        }
    }

    public void Refresh(string key)
    {
        TryLoadManifest();

        if (_cacheManifest is null)
            return;

        if (!_cacheManifest.TryGetValue(key, out var manifestEntry))
            return;

        if (!(manifestEntry.Expiry >= DateTimeOffset.UtcNow) && manifestEntry.Renewal == null)
            return;

        manifestEntry = manifestEntry with
        {
            Expiry = DateTimeOffset.UtcNow + manifestEntry.Renewal
        };

        _cacheManifest[key] = manifestEntry;
    }


    public async Task RefreshAsync(string key, CancellationToken token = new())
    {
        await TryLoadManifestAsync();

        if (_cacheManifest is null)
            return;

        if (!_cacheManifest.TryGetValue(key, out var manifestEntry))
            return;

        if (!(manifestEntry.Expiry >= DateTimeOffset.UtcNow) && manifestEntry.Renewal == null)
            return;

        manifestEntry = manifestEntry with
        {
            Expiry = DateTimeOffset.UtcNow + manifestEntry.Renewal
        };

        _cacheManifest[key] = manifestEntry;
    }


    public void Remove(string key)
    {
        TryLoadManifest();

        if (_cacheManifest is null)
            return;

        if (!_cacheManifest.TryRemove(key, out var manifestEntry))
            return;

        if (!_fileLock.TryRemove(manifestEntry.FileName, out var lockObj))
            return;

        using (lockObj.WriterLock())
        {
            var path = Path.Combine(_options.DirectoryPath, manifestEntry.FileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }


    public async Task RemoveAsync(string key, CancellationToken token = new())
    {
        await TryLoadManifestAsync();

        if (_cacheManifest is null)
            return;

        if (!_cacheManifest.TryRemove(key, out var manifestEntry))
            return;

        if (!_fileLock.TryRemove(manifestEntry.FileName, out var lockObj))
            return;

        using (await lockObj.WriterLockAsync(token))
        {
            var path = Path.Combine(_options.DirectoryPath, manifestEntry.FileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// Saves the cache manifest to the file system.
    /// </summary>
    /// <returns></returns>
    public void SaveManifest()
    {
        _manifestLock.Wait();
        try
        {
            if (!Directory.Exists(_options.DirectoryPath))
            {
                Directory.CreateDirectory(_options.DirectoryPath);
            }

            SerializeFile();
        }
        finally
        {
            _manifestLock.Release();
            LogManifestSaved();
        }
    }

    /// <summary>
    /// Saves the cache manifest to the file system.
    /// </summary>
    /// <returns></returns>
    public async Task SaveManifestAsync()
    {
        await _manifestLock.WaitAsync();
        try
        {
            if (!Directory.Exists(_options.DirectoryPath))
            {
                Directory.CreateDirectory(_options.DirectoryPath);
            }

            await SerializeFileAsync();
        }
        finally
        {
            _manifestLock.Release();
            LogManifestSaved();
        }
    }

    public void RemoveExpired()
    {
        TryLoadManifest();

        if (_cacheManifest is null)
            return;

        var removed = 0;
        foreach (var (key, manifestEntry) in _cacheManifest)
        {
            if (manifestEntry.Expiry != null && manifestEntry.Expiry >= DateTimeOffset.UtcNow)
                continue;

            if (!_fileLock.TryRemove(manifestEntry.FileName, out var lockObj))
                continue;

            using (lockObj.WriterLock())
            {
                var path = Path.Combine(_options.DirectoryPath, manifestEntry.FileName);
                if (!File.Exists(path))
                    continue;
                File.Delete(path);
                _cacheManifest.Remove(key, out _);
                removed++;
            }
        }

        if (removed > 0)
        {
            _logger.LogDebug(
                "Evicted {DeletedCacheEntryCount} expired entries from cache",
                removed
            );

            LogRemovedExpiredEntries();
        }
    }

    public async Task RemoveExpiredAsync()
    {
        await TryLoadManifestAsync();

        if (_cacheManifest is null)
            return;

        var removed = 0;
        foreach (var (key, manifestEntry) in _cacheManifest)
        {
            if (manifestEntry.Expiry != null && manifestEntry.Expiry >= DateTimeOffset.UtcNow)
                continue;

            if (!_fileLock.TryRemove(manifestEntry.FileName, out var lockObj))
                continue;

            using (await lockObj.WriterLockAsync())
            {
                var path = Path.Combine(_options.DirectoryPath, manifestEntry.FileName);
                if (!File.Exists(path))
                    continue;
                File.Delete(path);
                _cacheManifest.Remove(key, out _);
                removed++;
            }
        }

        if (removed > 0)
        {
            _logger.LogDebug(
                "Evicted {DeletedCacheEntryCount} expired entries from cache",
                removed
            );

            LogRemovedExpiredEntries();
        }
    }

    private void SerializeFile()
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(_cacheManifest,
            _jsonTypeInfo);
        File.WriteAllBytes(_manifestPath, data);
    }

    private async Task SerializeFileAsync()
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(_cacheManifest,
            _jsonTypeInfo);
        await File.WriteAllBytesAsync(_manifestPath, data);
    }

    private ConcurrentDictionary<string, ManifestEntry>? DeserializeFile()
    {
        var data = File.ReadAllBytes(_manifestPath);
        using var memStream = new MemoryStream(data);
        return (ConcurrentDictionary<string, ManifestEntry>?)JsonSerializer.Deserialize(memStream,
            _jsonTypeInfo);
    }

    private async Task<ConcurrentDictionary<string, ManifestEntry>?> DeserializeFileAsync()
    {
        var data = await File.ReadAllBytesAsync(_manifestPath);
        using var memStream = new MemoryStream(data);
        return (ConcurrentDictionary<string, ManifestEntry>?)await JsonSerializer.DeserializeAsync(memStream,
            _jsonTypeInfo);
    }

    private void TryLoadManifest()
    {
        //Avoid unnecessary lock contention way after manifest is loaded by checking before lock
        if (_cacheManifest is not null)
            return;

        _manifestLock.Wait();
        try
        {
            //Check that once we have lock (due to a race condition on the outer check) that we still need to load the manifest
            if (_cacheManifest is not null)
                return;

            if (File.Exists(_manifestPath))
            {
                _cacheManifest = DeserializeFile();
                _cacheManifest ??= new ConcurrentDictionary<string, ManifestEntry>();
            }
            else
            {
                if (!Directory.Exists(_options.DirectoryPath))
                {
                    Directory.CreateDirectory(_options.DirectoryPath);
                }

                _cacheManifest = new ConcurrentDictionary<string, ManifestEntry>();
                SerializeFile();
            }
        }
        finally
        {
            _manifestLock.Release();
        }
    }

    private async Task TryLoadManifestAsync()
    {
        //Avoid unnecessary lock contention way after manifest is loaded by checking before lock
        if (_cacheManifest is not null)
            return;

        await _manifestLock.WaitAsync();
        try
        {
            //Check that once we have lock (due to a race condition on the outer check) that we still need to load the manifest
            if (_cacheManifest is not null)
                return;

            if (File.Exists(_manifestPath))
            {
                _cacheManifest = await DeserializeFileAsync();
                _cacheManifest ??= new ConcurrentDictionary<string, ManifestEntry>();
            }
            else
            {
                if (!Directory.Exists(_options.DirectoryPath))
                {
                    Directory.CreateDirectory(_options.DirectoryPath);
                }

                _cacheManifest = new ConcurrentDictionary<string, ManifestEntry>();
                await SerializeFileAsync();
            }
        }
        finally
        {
            _manifestLock.Release();
        }
    }


    private async Task BackgroundRemovingExpired()
    {
        try
        {
            var cancellationToken = _removingExpiredCancellationTokenSource.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(
                    _options.RemoveExpiredInterval ?? _options.DefaultRemovedExpiredInterval,
                    cancellationToken
                );
                await RemoveExpiredAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }


    private async Task BackgroundManifestSaving()
    {
        try
        {
            var cancellationToken = _manifestSavingCancellationTokenSource.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(
                    _options.ManifestSaveInterval ?? _options.DefaultManifestSaveInterval,
                    cancellationToken
                );
                await SaveManifestAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    [LoggerMessage(Message = "Manifest saved", SkipEnabledCheck = true, Level = LogLevel.Information)]
    partial void LogManifestSaved();

    [LoggerMessage(Message = "Removed expired entries", SkipEnabledCheck = true, Level = LogLevel.Information)]
    partial void LogRemovedExpiredEntries();
}