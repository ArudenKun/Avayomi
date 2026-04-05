using System;
using System.IO;
using System.Security.Cryptography;
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
    private const string SaltFileName = "anilist.salt";

    private static readonly string MachineEntropy =
        Environment.MachineName + AvayomiCoreConsts.Name;

    private readonly ILogger<TokenService> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private string _token = string.Empty;
    private byte[]? _cachedSalt;

    public TokenService(ILogger<TokenService> logger)
    {
        _logger = logger;
    }

    private string StorePath { get; } = AvayomiCoreConsts.Paths.DataDir.Combine(TokenFileName);
    private string SaltPath { get; } = AvayomiCoreConsts.Paths.DataDir.Combine(SaltFileName);

    public string Get()
    {
        if (!string.IsNullOrEmpty(_token))
            return _token;

        if (!File.Exists(StorePath))
            return string.Empty;

        try
        {
            var encryptedBase64 = JsonSerializer.Deserialize<string>(File.ReadAllText(StorePath));
            _token = string.IsNullOrEmpty(encryptedBase64)
                ? string.Empty
                : Decrypt(encryptedBase64);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read or decrypt the token from storage.");
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
            if (!string.IsNullOrEmpty(_token))
                return _token;

            var encryptedBase64 = JsonSerializer.Deserialize<string>(
                await File.ReadAllTextAsync(StorePath)
            );
            _token = string.IsNullOrEmpty(encryptedBase64)
                ? string.Empty
                : Decrypt(encryptedBase64);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read or decrypt the token from storage");
            _token = string.Empty;
        }
        finally
        {
            _fileLock.Release();
        }

        return _token;
    }

    public async Task SaveAsync(string accessToken, bool persist = true)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return;

        await _fileLock.WaitAsync();
        try
        {
            _token = accessToken;

            if (persist)
            {
                var directory = Path.GetDirectoryName(StorePath) ?? string.Empty;
                DirectoryHelper.CreateIfNotExists(directory);
                var encryptedBase64 = Encrypt(accessToken);
                await using var stream = File.Create(StorePath);
                await JsonSerializer.SerializeAsync(stream, encryptedBase64);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to securely save the access token to storage.");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task ClearAsync()
    {
        _token = string.Empty;

        await _fileLock.WaitAsync();
        try
        {
            FileHelper.DeleteIfExists(StorePath);
            FileHelper.DeleteIfExists(SaltPath);
            _cachedSalt = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete token/salt files during logout.");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    // --- CRYPTOGRAPHY CORE ---

    private byte[] GetOrCreateSalt()
    {
        if (_cachedSalt is not null)
            return _cachedSalt;

        if (File.Exists(SaltPath))
        {
            _cachedSalt = File.ReadAllBytes(SaltPath);
            return _cachedSalt;
        }

        _cachedSalt = RandomNumberGenerator.GetBytes(16);

        var directory = Path.GetDirectoryName(SaltPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(SaltPath, _cachedSalt);
        return _cachedSalt;
    }

    private string Encrypt(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var salt = GetOrCreateSalt();

        // Modern, static allocation-free key derivation
        var key = Rfc2898DeriveBytes.Pbkdf2(
            MachineEntropy,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32
        ); // 32 bytes = 256-bit AES Key

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();

        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plainBytes, 0, plainBytes.Length);
            cs.FlushFinalBlock();
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    private string Decrypt(string cipherText)
    {
        var fullCipherBytes = Convert.FromBase64String(cipherText);
        var salt = GetOrCreateSalt();

        // Modern, static allocation-free key derivation
        var key = Rfc2898DeriveBytes.Pbkdf2(
            MachineEntropy,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32
        );

        using var aes = Aes.Create();
        aes.Key = key;

        var iv = new byte[aes.BlockSize / 8];
        Array.Copy(fullCipherBytes, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(
            fullCipherBytes,
            iv.Length,
            fullCipherBytes.Length - iv.Length
        );
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}
