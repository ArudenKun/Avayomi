using System.Buffers;

namespace Core.Extensions;

public static class CommonExtensions
{
    public static string ToMemoryMensurableUnit(this double bytes)
    {
        var kb = bytes / 1024; // · 1024 Bytes = 1 Kilobyte
        var mb = kb / 1024; // · 1024 Kilobytes = 1 Megabyte
        var gb = mb / 1024; // · 1024 Megabytes = 1 Gigabyte
        var tb = gb / 1024; // · 1024 Gigabytes = 1 Terabyte

        var result =
            tb > 1
                ? $"{tb:0.##}TB"
                : gb > 1
                    ? $"{gb:0.##}GB"
                    : mb > 1
                        ? $"{mb:0.##}MB"
                        : kb > 1
                            ? $"{kb:0.##}KB"
                            : $"{bytes:0.##}B";

        result = result.Replace("/", ".");

        return result;
    }

    public static string ToMemoryMensurableUnit(this long bytes)
    {
        return ((double)bytes).ToMemoryMensurableUnit();
    }

    public static async Task CopyToAsync(
        this Stream source,
        Stream destination,
        IProgress<double>? progress = null,
        long totalLength = 0,
        int bufferSize = 0x1000,
        CancellationToken cancellationToken = default
    )
    {
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        int bytesRead;
        long totalRead = 0;
        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            totalRead += bytesRead;
            progress?.Report(totalRead / (double)totalLength * 100 / 100);
        }

        ArrayPool<byte>.Shared.Return(buffer);
    }
}
