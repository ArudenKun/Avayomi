using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using AutoInterfaceAttributes;
using Desktop.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace Desktop.Services.Caching;

[AutoInterface(Inheritance = [typeof(IDistributedCache), typeof(IDisposable)])]
public sealed class FileCache : IFileCache
{
    private readonly Task _backgroundManifestSavingTask;
    private readonly Task _backgroundRemovingExpiredTask;
    private readonly ConcurrentDictionary<string, AsyncReaderWriterLock> _fileLock;

    private readonly FileCacheOptions _options;
    private readonly ILogger<FileCache> _logger;
    private readonly IFileSystemService _fileSystemService;
    private readonly JsonTypeInfo _rootTypeInfo;

    private readonly SemaphoreSlim _manifestLock = new(1, 1);
    private readonly string _manifestPath;
    private readonly CancellationTokenSource _manifestSavingCancellationTokenSource;

    private readonly CancellationTokenSource _removingExpiredCancellationTokenSource;

    private ConcurrentDictionary<string, ManifestEntry>? _cacheManifest;
    private bool _disposed;

    public FileCache(
        FileCacheOptions options,
        ILogger<FileCache> logger,
        IFileSystemService fileSystemService,
        JsonSerializerOptions jsonSerializerOptions
    )
    {
        _options = options;
        _logger = logger;
        _fileSystemService = fileSystemService;
        _rootTypeInfo = jsonSerializerOptions.GetTypeInfo(
            typeof(ConcurrentDictionary<string, ManifestEntry>)
        );
        _manifestPath = _fileSystemService.Path.Combine(options.DirectoryPath, "manifest.json");
        _fileLock = new ConcurrentDictionary<string, AsyncReaderWriterLock>(StringComparer.Ordinal);

        _removingExpiredCancellationTokenSource = new CancellationTokenSource();
        _manifestSavingCancellationTokenSource = new CancellationTokenSource();
        _backgroundRemovingExpiredTask = BackgroundRemovingExpired();
        _backgroundManifestSavingTask = BackgroundManifestSaving();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _removingExpiredCancellationTokenSource.Cancel();
        if (!_backgroundRemovingExpiredTask.IsFaulted)
            _backgroundRemovingExpiredTask.Wait();

        _manifestSavingCancellationTokenSource.Cancel();
        if (!_backgroundManifestSavingTask.IsFaulted)
            _backgroundManifestSavingTask.Wait();

        SaveManifest();
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

            var path = _fileSystemService.Path.Combine(
                _options.DirectoryPath,
                manifestEntry.FileName
            );
            if (_fileSystemService.File.Exists(path))
            {
                using var stream = _fileSystemService.File.OpenRead(path);
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

    public Task<byte[]?> GetAsync(string key, CancellationToken token = new())
    {
        return Task.Run(() => Get(key), token);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        TryLoadManifest();

        if (_cacheManifest is null)
            return;

        DateTimeOffset? expiry = null;
        TimeSpan? renewal = null;

        if (options.AbsoluteExpiration.HasValue)
            expiry = options.AbsoluteExpiration.Value.ToUniversalTime();
        else if (options.AbsoluteExpirationRelativeToNow.HasValue)
            expiry = DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);

        if (options.SlidingExpiration.HasValue)
        {
            renewal = options.SlidingExpiration.Value;
            expiry = (expiry ?? DateTimeOffset.UtcNow) + renewal;
        }

        //Update the manifest entry with the new expiry
        if (_cacheManifest.TryGetValue(key, out var manifestEntry))
            manifestEntry = manifestEntry with { Expiry = expiry, Renewal = renewal };
        else
            manifestEntry = new ManifestEntry(MD5HashHelper.ComputeHash(key), expiry, renewal);

        _cacheManifest[key] = manifestEntry;

        var lockObj = _fileLock.GetOrAdd(
            manifestEntry.FileName,
            static _ => new AsyncReaderWriterLock()
        );

        using (lockObj.WriterLock())
        {
            var path = _fileSystemService.Path.Combine(
                _options.DirectoryPath,
                manifestEntry.FileName
            );
            using var stream = _fileSystemService.File.OpenWrite(path);
            using var memStream = new MemoryStream(value);
            memStream.CopyTo(stream);
        }
    }

    public Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = new()
    )
    {
        return Task.Run(() => Set(key, value, options), token);
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

    public Task RefreshAsync(string key, CancellationToken token = new())
    {
        return Task.Run(() => Refresh(key), token);
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
            var path = _fileSystemService.Path.Combine(
                _options.DirectoryPath,
                manifestEntry.FileName
            );
            if (_fileSystemService.File.Exists(path))
                _fileSystemService.File.Delete(path);
        }
    }

    public Task RemoveAsync(string key, CancellationToken token = new())
    {
        return Task.Run(() => Remove(key), token);
    }

    /// <summary>
    ///     Saves the cache manifest to the file system.
    /// </summary>
    /// <returns></returns>
    public void SaveManifest()
    {
        _manifestLock.Wait();
        try
        {
            if (!_fileSystemService.Directory.Exists(_options.DirectoryPath))
                _fileSystemService.Directory.CreateDirectory(_options.DirectoryPath);

            SerializeFile();
        }
        finally
        {
            _manifestLock.Release();
        }
    }

    /// <summary>
    ///     Saves the cache manifest to the file system.
    /// </summary>
    /// <returns></returns>
    public async Task SaveManifestAsync()
    {
        await _manifestLock.WaitAsync();
        try
        {
            if (!_fileSystemService.Directory.Exists(_options.DirectoryPath))
                _fileSystemService.Directory.CreateDirectory(_options.DirectoryPath);

            await SerializeFileAsync();
        }
        finally
        {
            _manifestLock.Release();
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
                var path = _fileSystemService.Path.Combine(
                    _options.DirectoryPath,
                    manifestEntry.FileName
                );
                if (!_fileSystemService.File.Exists(path))
                    continue;
                _fileSystemService.File.Delete(path);
                _cacheManifest.Remove(key, out _);
                removed++;
            }
        }

        if (removed > 0)
            _logger.LogDebug(
                "Evicted {DeletedCacheEntryCount} expired entries from cache",
                removed
            );
    }

    public Task RemoveExpiredAsync()
    {
        return Task.Run(RemoveExpired);
    }

    private void SerializeFile() =>
        _fileSystemService.File.WriteAllBytes(
            _manifestPath,
            JsonSerializer.SerializeToUtf8Bytes(_cacheManifest, _rootTypeInfo)
        );

    private async Task SerializeFileAsync() =>
        await _fileSystemService.File.WriteAllBytesAsync(
            _manifestPath,
            JsonSerializer.SerializeToUtf8Bytes(_cacheManifest, _rootTypeInfo)
        );

    private ConcurrentDictionary<string, ManifestEntry>? DeserializeFile()
    {
        using var stream = _fileSystemService.File.OpenRead(_manifestPath);
        return (ConcurrentDictionary<string, ManifestEntry>?)
            JsonSerializer.Deserialize(stream, _rootTypeInfo);
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

            if (_fileSystemService.File.Exists(_manifestPath))
            {
                _cacheManifest = DeserializeFile();
                _cacheManifest ??= new ConcurrentDictionary<string, ManifestEntry>();
            }
            else
            {
                if (!_fileSystemService.Directory.Exists(_options.DirectoryPath))
                    _fileSystemService.Directory.CreateDirectory(_options.DirectoryPath);

                _cacheManifest = new ConcurrentDictionary<string, ManifestEntry>();
                SerializeFile();
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
                _logger.LogInformation("Removed expired entries");
            }
        }
        catch (OperationCanceledException) { }
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
                _logger.LogInformation("FileCache manifest saved");
            }
        }
        catch (OperationCanceledException) { }
    }
}
