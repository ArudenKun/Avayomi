using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AsyncImageLoader;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Replicant;

namespace Avayomi.Utilities;

public sealed class ReplicantImageLoader : IAsyncImageLoader, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly HttpCache _httpCache;

    public ReplicantImageLoader(
        string cacheDir,
        HttpClient? httpClient = null,
        ILogger? logger = null
    )
    {
        var socketHandler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        };
        httpClient ??= new HttpClient(socketHandler);
        _httpCache = new HttpCache(cacheDir, httpClient);
        _logger = logger ?? NullLogger.Instance;
    }

    public async Task<Bitmap?> ProvideImageAsync(string url)
    {
        var internalOrCachedBitmap =
            await LoadFromLocalAsync(url, null).ConfigureAwait(false)
            ?? await LoadFromInternalAsync(url).ConfigureAwait(false);
        if (internalOrCachedBitmap is not null)
            return internalOrCachedBitmap;

        try
        {
            var externalBytes = await _httpCache.BytesAsync(url).ConfigureAwait(false);
            using var memoryStream = new MemoryStream(externalBytes);
            var bitmap = new Bitmap(memoryStream);
            return bitmap;
        }
        catch (Exception e)
        {
            _logger.LogError(
                "Failed to resolve image: {RequestUri}\nException: {Exception}",
                url,
                e
            );

            return null;
        }
    }

    /// <summary>
    /// The url maybe is local file url, so if file exists, we got a Bitmap
    /// </summary>
    /// <param name="url">Url to load</param>
    /// <param name="storageProvider">Avalonia's storage provider</param>
    private async Task<Bitmap?> LoadFromLocalAsync(string url, IStorageProvider? storageProvider)
    {
        if (File.Exists(url))
            return new Bitmap(url);

        if (storageProvider is null)
            return null;
        if (
            !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("file" or "content")
        )
            return null;

        try
        {
            var fileInfo = await storageProvider.TryGetFileFromPathAsync(uri);
            if (fileInfo is null)
                return null;
            await using var fileStream = await fileInfo.OpenReadAsync();
            return new Bitmap(fileStream);
        }
        catch (Exception e)
        {
            _logger.LogInformation(
                "Failed to resolve local image via storage provider with uri: {RequestUri}\nException: {Exception}",
                url,
                e
            );
            return null;
        }
    }

    /// <summary>
    ///     Receives image bytes from an internal source (for example, from the disk).
    ///     This data will be NOT cached globally (because it is assumed that it is already in internal source us and does not
    ///     require global caching)
    /// </summary>
    /// <param name="url">Target url</param>
    /// <returns>Bitmap</returns>
    private Task<Bitmap?> LoadFromInternalAsync(string url)
    {
        try
        {
            var uri = url.StartsWith("/")
                ? new Uri(url, UriKind.Relative)
                : new Uri(url, UriKind.RelativeOrAbsolute);

            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                return Task.FromResult<Bitmap?>(null);

            if (uri is { IsAbsoluteUri: true, IsFile: true })
                return Task.FromResult(new Bitmap(uri.LocalPath))!;

            return Task.FromResult(new Bitmap(AssetLoader.Open(uri)))!;
        }
        catch (Exception e)
        {
            _logger.LogInformation(
                "Failed to resolve image from request with uri: {RequestUri}\nException: {Exception}",
                url,
                e
            );
            return Task.FromResult<Bitmap?>(null);
        }
    }

    public void Dispose()
    {
        _httpCache.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _httpCache.DisposeAsync();
    }
}
