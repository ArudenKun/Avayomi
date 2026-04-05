using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Avayomi.Core;
using Avayomi.Core.Extensions;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.IO;

namespace Avayomi.Services;

[AutoExtractInterface]
[ExposeServices(typeof(ITokenService))]
public sealed class TokenService : ITokenService, ISingletonDependency
{
    private const string TokenFileName = "anilist.json";

    private readonly ILogger<TokenService> _logger;

    // Semaphore to prevent concurrent read/write issues on the file
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private string _token = string.Empty;

    public TokenService(ILogger<TokenService> logger)
    {
        _logger = logger;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Get());

    private string StorePath { get; } = AvayomiCoreConsts.Paths.DataDir.Combine(TokenFileName);

    public string Get()
    {
        if (!string.IsNullOrEmpty(_token))
            return _token;

        if (!File.Exists(StorePath))
            return string.Empty;

        try
        {
            var base64 = JsonSerializer.Deserialize<string>(File.ReadAllText(StorePath));
            _token = string.IsNullOrEmpty(base64) ? string.Empty : base64.DecodeBase64();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read or deserialize the token from storage.");
            _token = string.Empty;
        }

        return _token;
    }

    public async Task<string> GetAsync()
    {
        if (!string.IsNullOrEmpty(_token))
            return _token;

        if (!File.Exists(StorePath))
            return string.Empty;

        await _fileLock.WaitAsync();
        try
        {
            // Double-check locking pattern
            if (!string.IsNullOrEmpty(_token))
                return _token;

            var base64 = JsonSerializer.Deserialize<string>(await File.ReadAllTextAsync(StorePath));
            _token = string.IsNullOrEmpty(base64) ? string.Empty : base64.DecodeBase64();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to read or deserialize the token from storage asynchronously."
            );
            _token = string.Empty;
        }
        finally
        {
            _fileLock.Release();
        }

        return _token;
    }

    public async Task SaveAsync(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return;

        await _fileLock.WaitAsync();
        try
        {
            // Ensure the directory exists before trying to create a file inside it
            var directory = Path.GetDirectoryName(StorePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(StorePath);
            var bytes = Encoding.UTF8.GetBytes(accessToken);
            var base64 = Convert.ToBase64String(bytes);
            await JsonSerializer.SerializeAsync(stream, base64);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save the access token to storage.");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    // Optional but Recommended: Added a Logout method
    public async Task ClearAsync()
    {
        _token = string.Empty;

        await _fileLock.WaitAsync();
        try
        {
            FileHelper.DeleteIfExists(StorePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete token file during logout.");
        }
        finally
        {
            _fileLock.Release();
        }
    }
}
