using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Avayomi.Core.Options;

[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
internal sealed class OptionsMutableJsonFileStore<T> : IOptionsMutableStore<T>
{
    private readonly IOptionsMonitor<MutableOptionsWrapper> _optionsMonitor;

    private static readonly JsonWriterOptions JsonWriterOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonReaderOptions JsonReaderOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public OptionsMutableJsonFileStore(IOptionsMonitor<MutableOptionsWrapper> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public async Task UpdateAsync(string section, T? options)
    {
        var encodedSection = JsonEncodedText.Encode(section);
        await using var stream = new FileStream(
            _optionsMonitor.CurrentValue.FilePath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None
        );
        var buffer = ArrayPool<byte>.Shared.Rent((int)stream.Length);
        try
        {
#if NETCOREAPP2_1_OR_GREATER
            _ = stream.Read(buffer);
#else
            _ = stream.Read(buffer, 0, buffer.Length);
#endif
            ReadOnlySpan<byte> utf8Json = buffer.AsSpan();

            // Check BOM
#if NETCOREAPP2_1_OR_GREATER
            var utf8Bom = Encoding.UTF8.Preamble;
#else
            ReadOnlySpan<byte> utf8bom = Encoding.UTF8.GetPreamble();
#endif
            if (utf8Json.StartsWith(utf8Bom))
            {
#pragma warning disable IDE0057
                utf8Json = utf8Json.Slice(utf8Bom.Length);
#pragma warning restore IDE0057
                _ = stream.Seek(utf8Bom.Length, SeekOrigin.Begin);
            }
            else
            {
                stream.Position = 0;
            }

            var reader = new Utf8JsonReader(
                utf8Json.Length > 0 ? utf8Json : "{}"u8,
                JsonReaderOptions
            );
            var currentJson = JsonElement.ParseValue(ref reader);
            var writer = new Utf8JsonWriter(stream, JsonWriterOptions);

            writer.WriteStartObject(); // {
            bool isWritten = false;
            var serializedOptionsValue = JsonSerializer.SerializeToElement(options);
            foreach (var element in currentJson.EnumerateObject())
            {
                if (!element.NameEquals(encodedSection.EncodedUtf8Bytes))
                {
                    element.WriteTo(writer);
                    continue;
                }

                writer.WritePropertyName(encodedSection);
                serializedOptionsValue.WriteTo(writer);
                isWritten = true;
            }

            if (!isWritten)
            {
                writer.WritePropertyName(encodedSection);
                serializedOptionsValue.WriteTo(writer);
            }

            writer.WriteEndObject(); // }

            await writer.FlushAsync();
            stream.SetLength(stream.Position);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
