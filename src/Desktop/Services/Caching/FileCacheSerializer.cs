using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion.Serialization;

namespace Desktop.Services.Caching;

public class FileCacheSerializer : IFusionCacheSerializer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public FileCacheSerializer(JsonSerializerOptions jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public byte[] Serialize<T>(T? obj)
    {
        var typeInfo = _jsonSerializerOptions.GetTypeInfo(typeof(T));
        return JsonSerializer.SerializeToUtf8Bytes(obj, typeInfo);
    }

    public T? Deserialize<T>(byte[] data)
    {
        var typeInfo = _jsonSerializerOptions.GetTypeInfo(typeof(T));
        return (T?)JsonSerializer.Deserialize(data, typeInfo);
    }

    public async ValueTask<byte[]> SerializeAsync<T>(T? obj)
    {
        var typeInfo = _jsonSerializerOptions.GetTypeInfo(typeof(T));
        var data = JsonSerializer.SerializeToUtf8Bytes(obj, typeInfo);
        return await ValueTask.FromResult(data);
    }

    public async ValueTask<T?> DeserializeAsync<T>(byte[] data)
    {
        var typeInfo = _jsonSerializerOptions.GetTypeInfo(typeof(T));
        using var stream = new MemoryStream(data);
        return (T?)await JsonSerializer.DeserializeAsync(stream, typeInfo);
    }
}
