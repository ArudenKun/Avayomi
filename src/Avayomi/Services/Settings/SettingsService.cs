using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Avayomi.Services.Settings;

[AutoExtractInterface]
[ExposeServices(typeof(ISettingsService))]
internal sealed class SettingsService : ISettingsService, IDisposable, ISingletonDependency
{
    private readonly ILogger<SettingsService> _logger;
    private readonly ConcurrentDictionary<Type, Lazy<object>> _settings = new();
    private readonly ConcurrentDictionary<Type, JsonTypeInfo> _settingsJsonTypeInfo = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the SettingsService.</summary>
    /// <summary>
    /// Initializes an instance of <see cref="SettingsService" />.
    /// </summary>
    public SettingsService(
        IOptions<SettingsServiceOptions> settingsServiceOptions,
        ILogger<SettingsService> logger
    )
    {
        _logger = logger;
        FilePath = settingsServiceOptions.Value.FilePath;
        _jsonSerializerOptions = new JsonSerializerOptions(
            settingsServiceOptions.Value.JsonSerializerOptions
        )
        {
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true,
        };
    }

    public static ISettingsService Create() =>
        new SettingsService(
            Options.Create(new SettingsServiceOptions()),
            NullLogger<SettingsService>.Instance
        );

    public string FilePath { get; }

    public bool DisableSave { get; set; }

    public event EventHandler<SettingsErrorEventArgs>? ErrorOccurred;

    public T Get<T>()
        where T : class, new() =>
        (T)_settings.GetOrAdd(typeof(T), key => new Lazy<object>(() => Load(key))).Value;

    public void Save()
    {
        if (DisableSave)
        {
            _logger.LogInformation("Saving has been disabled");
            return;
        }

        var settingsList = _settings.ToArray(); // Snapshot of current loaded settings
        if (settingsList.Length < 1)
            return;

        _logger.LogInformation("Saving");
        try
        {
            JsonObject? rootNode = null;

            // 1. Try to load existing file to preserve settings not currently in memory
            if (File.Exists(FilePath))
            {
                try
                {
                    using var stream = new FileStream(
                        FilePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read
                    );
                    // Parse into a mutable JsonNode
                    rootNode = JsonNode.Parse(stream)?.AsObject();
                }
                catch (Exception ex)
                {
                    // _logger.LogError(ex, "Error reading settings file");
                    OnErrorOccurred(
                        new SettingsErrorEventArgs(ex, SettingsServiceAction.Save, FilePath)
                    );
                }
            }
            else
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(FilePath)
                        ?? throw new DirectoryNotFoundException("Directory not found: " + FilePath)
                );
            }

            // 2. Initialize if null (new file or corrupted read)
            rootNode ??= new JsonObject();

            // 3. Update the JsonObject with current in-memory settings
            try
            {
                UpdateJsonNode(rootNode, settingsList);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(
                    new SettingsErrorEventArgs(ex, SettingsServiceAction.Save, FilePath)
                );
                // If update failed, reset to empty to ensure we at least save the current state
                rootNode = new JsonObject();
                UpdateJsonNode(rootNode, settingsList);
            }

            // 4. Write back to disk
            using (
                var stream = new FileStream(
                    FilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Write
                )
            )
            {
                using var writer = new Utf8JsonWriter(
                    stream,
                    new JsonWriterOptions { Indented = true }
                );
                rootNode.WriteTo(writer, _jsonSerializerOptions);
            }

            _logger.LogInformation("Saved");
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new SettingsErrorEventArgs(ex, SettingsServiceAction.Save, FilePath));
        }
    }

    private void UpdateJsonNode(JsonObject root, KeyValuePair<Type, Lazy<object>>[] settingsList)
    {
        foreach (var kvp in settingsList)
        {
            // Ensure we only save values that have actually been instantiated
            if (kvp.Value.IsValueCreated)
            {
                var typeKey = GetTypeKey(kvp.Key);
                var settingObj = kvp.Value.Value;
                var jsonTypeInfo = _settingsJsonTypeInfo.GetOrAdd(
                    kvp.Key,
                    k => _jsonSerializerOptions.GetTypeInfo(k)
                );

                // Serialize the specific setting object to a node
                var settingNode = JsonSerializer.SerializeToNode(settingObj, jsonTypeInfo);

                // Update or Add to the root object
                root[typeKey] = settingNode;
            }
        }
    }

    /// <summary>Calls save to ensure that the latest changes are persisted.</summary>
    public void Dispose()
    {
        Save();
    }

    private void OnErrorOccurred(SettingsErrorEventArgs e)
    {
        _logger.LogError(e.Error, "An error occurred on [{Action}]", e.Action);
        ErrorOccurred?.Invoke(this, e);
    }

    private object Load(Type type)
    {
        object? settingObject = null;
        try
        {
            settingObject = LoadCore(type);
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new SettingsErrorEventArgs(ex, SettingsServiceAction.Open, FilePath));
        }

        return settingObject ?? Activator.CreateInstance(type)!;
    }

    private object? LoadCore(Type type)
    {
        if (!File.Exists(FilePath))
            return null;

        var jsonTypeInfo = _settingsJsonTypeInfo.GetOrAdd(
            type,
            k => _jsonSerializerOptions.GetTypeInfo(k)
        );

        try
        {
            using var stream = new FileStream(
                FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );

            // Parse the whole file structure
            var rootNode = JsonNode.Parse(stream);
            if (rootNode is JsonObject rootObj)
            {
                var typeKey = GetTypeKey(type);

                // Find the key corresponding to this type
                if (rootObj.TryGetPropertyValue(typeKey, out var section) && section != null)
                {
                    return section.Deserialize(jsonTypeInfo);
                }
            }
        }
        catch (JsonException)
        {
            // JSON might be corrupted; return null so a default instance is created.
            // The Save() method will handle overwriting the bad file later.
        }

        return null;
    }

    private static string GetTypeKey(Type type)
    {
        // Use FullName for unique identification, but if you only want
        // the hierarchy without namespaces, we build it manually.
        var parts = new List<string>();
        var current = type;
        while (current is not null)
        {
            parts.Add(current.Name);
            current = current.IsNested ? current.DeclaringType : null;
        }
        // Reverse to get [Parent, Child] order and join
        parts.Reverse();
        var name = string.Join(".", parts);
        // Safe suffix removal
        if (name.EndsWith("Settings", StringComparison.Ordinal) && name.Length > "Settings".Length)
        {
            return name[..^"Settings".Length];
        }

        return name;
    }
}
