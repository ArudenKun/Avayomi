using System.Text.Json.Serialization;
using Avayomi.Models;

namespace Avayomi;

[JsonSerializable(typeof(Settings))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
public sealed partial class GlobalJsonContext : JsonSerializerContext;