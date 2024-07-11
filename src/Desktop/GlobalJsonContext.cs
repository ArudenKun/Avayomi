using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Desktop.Models;
using Desktop.Services.Caching;
using ZiggyCreatures.Caching.Fusion.Internals.Distributed;

namespace Desktop;

[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(ConcurrentDictionary<string, ManifestEntry>))]
[JsonSerializable(typeof(FusionCacheDistributedEntry<byte[]>))]
[JsonSerializable(typeof(FusionCacheDistributedEntry<bool>))]
[JsonSerializable(typeof(FusionCacheDistributedEntry<string>))]
[JsonSerializable(typeof(FusionCacheDistributedEntry<int>))]
[JsonSerializable(typeof(FusionCacheDistributedEntry<Dictionary<string, string>>))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
public sealed partial class GlobalJsonContext : JsonSerializerContext;
