using Avalonia;
using Avalonia.Logging;

namespace Avayomi.Logging;

public static class AppBuilderExtensions
{
    /// <summary>
    /// Logs to Serilog with the given log level and areas.
    /// </summary>
    /// <param name="builder">The <see cref="Avalonia.AppBuilder"/>.</param>
    /// <param name="level">The level of the log messages to be written to Serilog.</param>
    /// <param name="areas">The areas to log.</param>
    /// <returns>The same instance of the <see cref="Avalonia.AppBuilder"/>.</returns>
    public static AppBuilder LogToSerilog(
        this AppBuilder builder,
        LogEventLevel level = LogEventLevel.Warning,
        params string[] areas
    )
    {
        Logger.Sink = new SerilogSink(level, areas);
        return builder;
    }
}
