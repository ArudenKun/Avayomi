using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using AutoInterfaceAttributes;
using Microsoft.Extensions.Logging;

namespace Desktop.Services;

[AutoInterface(Name = "ISettingsProvider")]
public abstract partial class BaseSettingsProvider<T> : ISettingsProvider<T>
    where T : class, new()
{
    private readonly ILogger _logger;
    private readonly JsonTypeInfo _rootTypeInfo;

    public BaseSettingsProvider(ILogger logger, IJsonTypeInfoResolver jsonTypeInfoResolver)
    {
        _logger = logger;
        _rootTypeInfo = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = jsonTypeInfoResolver
        }.GetTypeInfo(typeof(T));
    }

    /// <summary>
    /// Gets the path where settings are stored.
    /// </summary>
    public abstract string FilePath { get; }

    /// <summary>
    /// Gets the current settings.
    /// </summary>
    public T Value { get; private set; } = new();

    /// <summary>
    /// Saves settings into json file.
    /// </summary>
    public async Task SaveAsync()
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(Value, _rootTypeInfo);

        var dirPath = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(dirPath))
            Directory.CreateDirectory(dirPath);

        await File.WriteAllBytesAsync(FilePath, data);
        LogSuccessfullySaved(typeof(T).Name);
    }

    /// <summary>
    /// Saves settings into json file.
    /// </summary>
    public void Save()
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(Value, _rootTypeInfo);

        var dirPath = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(dirPath))
            Directory.CreateDirectory(dirPath);

        File.WriteAllBytes(FilePath, data);
        LogSuccessfullySaved(typeof(T).Name);
    }

    /// <summary>
    /// Loads the settings file if present, or creates a new object with default values.
    /// </summary>
    public void Load()
    {
        try
        {
            using var stream = File.OpenRead(FilePath);
            if (JsonSerializer.Deserialize(stream, _rootTypeInfo) is not T data)
            {
                Reset();
                return;
            }

            Value = data;
        }
        catch (DirectoryNotFoundException)
        {
            Reset();
        }
        catch (FileNotFoundException)
        {
            Reset();
        }
    }

    /// <summary>
    /// Loads the settings file if present, or creates a new object with default values.
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            await using var stream = File.OpenRead(FilePath);
            if (await JsonSerializer.DeserializeAsync(stream, _rootTypeInfo) is not T data)
            {
                Reset();
                return;
            }

            Value = data;
        }
        catch (DirectoryNotFoundException)
        {
            Reset();
        }
        catch (FileNotFoundException)
        {
            Reset();
        }
    }

    /// <summary>
    /// Resets the current settings to the default values
    /// </summary>
    public void Reset()
    {
        Value = new T();
    }

    [LoggerMessage(LogLevel.Information, "Settings {typeName} successfully saved")]
    partial void LogSuccessfullySaved(string typeName);
}
