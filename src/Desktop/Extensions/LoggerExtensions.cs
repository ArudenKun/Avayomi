using System;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace Desktop.Extensions;

public static class LoggerExtensions
{
    public static void LogException(
        this ILogger logger,
        string exceptionMessage,
        LogEventLevel logLevel = LogEventLevel.Error
    )
    {
        logger.Write(logLevel, exceptionMessage);
    }

    public static void LogException(
        this ILogger logger,
        Exception exception,
        LogEventLevel logLevel = LogEventLevel.Error,
        string? exceptionNote = null,
        string logType = "AppException"
    )
    {
        _LogException(
            exception,
            out var message,
            out var stackTrace,
            out var source,
            out var innerMessage,
            out var innerStackTrace,
            out var innerSource,
            out var format
        );
        logger.Write(
            logLevel,
            format,
            logType,
            message,
            stackTrace,
            source,
            innerMessage,
            innerStackTrace,
            innerSource,
            exceptionNote
        );
    }

    public static void LogException(
        this Microsoft.Extensions.Logging.ILogger logger,
        string exceptionMessage,
        LogLevel logLevel = LogLevel.Error
    )
    {
        logger.Log(logLevel, exceptionMessage);
    }

    public static void LogException(
        this Microsoft.Extensions.Logging.ILogger logger,
        Exception exception,
        LogLevel logLevel = LogLevel.Error,
        string? exceptionNote = null,
        string logType = "AppException"
    )
    {
        _LogException(
            exception,
            out var message,
            out var stackTrace,
            out var source,
            out var innerMessage,
            out var innerStackTrace,
            out var innerSource,
            out var format
        );
        logger.Log(
            logLevel,
            format,
            logType,
            message,
            stackTrace,
            source,
            innerMessage,
            innerStackTrace,
            innerSource,
            exceptionNote
        );
    }

    private static void _LogException(
        Exception exception,
        out string message,
        out string stackTrace,
        out string source,
        out string innerMessage,
        out string innerStackTrace,
        out string innerSource,
        out string format
    )
    {
        format =
            "{lt}: {Message} {StackTrace} {Source} {InnerMessage} {InnerStackTrace} {InnerSource} {ExceptionNote}";
        message = exception.Message;
        stackTrace = exception.StackTrace ?? ".";
        source = exception.Source ?? ".";
        innerMessage = exception.InnerException?.Message ?? ".";
        innerStackTrace = exception.InnerException?.StackTrace ?? ".";
        innerSource = exception.InnerException?.Source ?? ".";
    }
}
