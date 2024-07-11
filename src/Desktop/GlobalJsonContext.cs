using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Core.Caching;
using Desktop.Models;
using ZiggyCreatures.Caching.Fusion.Internals.Distributed;

namespace Desktop;

[JsonSerializable(typeof(Settings))]
[JsonSerializable(typeof(ConcurrentDictionary<string, ManifestEntry>))]
[JsonSerializable(typeof(FusionCacheDistributedEntry<byte[]>))]
[JsonSerializable(typeof(FusionCacheDistributedEntry<Dictionary<string, string>>))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
public sealed partial class GlobalJsonContext : JsonSerializerContext;
