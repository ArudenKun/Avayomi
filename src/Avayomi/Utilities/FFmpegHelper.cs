using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avayomi.Core;
using Avayomi.Core.Extensions;

namespace Avayomi.Utilities;

public static class FFmpegHelper
{
    /// <summary>
    /// Checks whether FFmpeg is available on the current system.
    /// </summary>
    /// <returns>True if FFmpeg is found; otherwise, false.</returns>
    public static bool IsFFmpegAvailable() => !FindFFmpegPath().IsNullOrEmpty();

    /// <summary>
    /// Finds the path to the FFmpeg executable.
    /// </summary>
    /// <returns>The path to the FFmpeg executable, or null if not found.</returns>
    public static string? FindFFmpegPath()
    {
        // Determine the binary name for the current OS
        string binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "ffmpeg.exe"
            : "ffmpeg";

        // Search the System PATH
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathVar))
        {
            // PathSeparator is ';' on Windows and ':' on Linux/macOS
            var paths = pathVar.Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, binaryName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }

        string bundlePath = AvayomiCoreConsts.Paths.ToolsDir.Combine(binaryName);
        if (File.Exists(bundlePath))
            return bundlePath;

        // Check common paths (Backups for macOS/Linux)
        // /opt/homebrew -> Apple Silicon
        string[] commonPaths = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? [@"C:\ffmpeg\bin", @"C:\Program Files\ffmpeg\bin"]
            : ["/usr/bin", "/usr/local/bin", "/opt/homebrew/bin"];

        return commonPaths
            .Select(path => Path.Combine(path, binaryName))
            .FirstOrDefault(File.Exists);
    }
}
