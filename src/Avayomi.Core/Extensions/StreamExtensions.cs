namespace Avayomi.Core.Extensions;

public static class StreamExtensions
{
    public static async ValueTask CopyToAsync(
        this Stream source,
        Stream destination,
        IProgress<double>? progress = null,
        long totalLength = 0,
        int bufferSize = 0x1000,
        CancellationToken cancellationToken = default
    )
    {
        var buffer = new byte[bufferSize];
        int bytesRead;
        long totalRead = 0;
        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            totalRead += bytesRead;
            progress?.Report(totalRead / (double)totalLength * 100 / 100);
        }
    }
}
