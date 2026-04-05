using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avayomi.Mpv.Native;
using Serilog;

namespace Avayomi.Controls;

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Information about a libmpv release on GitHub.
/// </summary>
public sealed class MpvReleaseInfo
{
    public string Tag { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public override string ToString() => string.IsNullOrEmpty(Title) ? Tag : Title;
}

/// <summary>
/// Responsible for ensuring a compatible libmpv binary is available to the
/// application. Handles downloading Windows builds and locating system
/// libraries on Unix-like platforms.
/// </summary>
public partial class MpvLibraryManager : ObservableObject
{
    private const string Repo = "zhongfly/mpv-winbuild";
    private static readonly HttpClient Client = new();
    private int _lastInstallerExitCode;

    private static MpvCacheEntry? _cache;

    private sealed class MpvCacheEntry
    {
        public string? ETag { get; set; }
        public List<MpvReleaseInfo>? Versions { get; set; }
        public DateTime LastUpdated { get; set; }

        public string? LatestETag { get; set; }
        public string? LatestReleaseJson { get; set; }
        public DateTime LastLatestUpdated { get; set; }
    }

    [JsonSourceGenerationOptions(WriteIndented = false)]
    [JsonSerializable(typeof(MpvCacheEntry))]
    private partial class MpvLibraryJsonContext : JsonSerializerContext;

    /// <summary>
    /// Event raised when libmpv usage should be terminated (e.g., before uninstallation).
    /// </summary>
    public event Action? RequestMpvTermination;

    /// <summary>
    /// Attempts to stop libmpv usage across the application.
    /// Broadcasts <see cref="RequestMpvTermination"/>.
    /// </summary>
    public void KillAllMpvActivity()
    {
        RequestMpvTermination?.Invoke();
    }

    /// <summary>
    /// Raised when an installation/upgrade/uninstall operation completes.
    /// </summary>
    public event EventHandler<InstallationCompletedEventArgs>? InstallationCompleted;

    /// <summary>
    /// Human readable status text for display in the UI.
    /// </summary>
    [ObservableProperty]
    private string _status = "Idle";

    private int _activeTaskCount;

    /// <summary>
    /// Indicates an ongoing operation (download or installation in progress).
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    partial void OnIsBusyChanged(bool value)
    {
        if (!value)
            UpdateStatusInternal();
    }

    /// <summary>
    /// Reports libmpv activity to update the status label.
    /// </summary>
    /// <param name="isActive">True if background libmpv activity is starting; false if it has stopped.</param>
    public void ReportActivity(bool isActive)
    {
        if (isActive)
            Interlocked.Increment(ref _activeTaskCount);
        else
            Interlocked.Decrement(ref _activeTaskCount);

        // Ensure count doesn't drop below zero due to race conditions or mismatched calls
        if (_activeTaskCount < 0)
            Interlocked.Exchange(ref _activeTaskCount, 0);

        UpdateStatusInternal();
    }

    private void UpdateStatusInternal()
    {
        if (IsBusy)
            return;

        if (_activeTaskCount > 0)
        {
            Status = $"libmpv is active ({_activeTaskCount} task(s))";
        }
        else if (
            string.IsNullOrEmpty(Status)
            || Status == "Idle"
            || Status.StartsWith("libmpv is active")
        )
        {
            Status = "Idle";
        }
    }

    /// <summary>
    /// True while a download is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isDownloading;

    /// <summary>
    /// Percentage (0-100) representing the current download progress.
    /// </summary>
    [ObservableProperty]
    private double _downloadProgress;

    /// <summary>
    /// Indicates if a library update or removal requires an application restart to complete.
    /// </summary>
    [ObservableProperty]
    private bool _isPendingRestart;

    /// <summary>
    /// Checks whether libmpv is locally installed.
    /// </summary>
    public bool IsLibraryInstalled()
    {
        if (OperatingSystem.IsAndroid())
        {
            if (NativeLibrary.TryLoad(MpvNativeLibrary.AndroidFileName, out var handle))
            {
                NativeLibrary.Free(handle);
                return true;
            }
        }
        return File.Exists(Path.Combine(AppContext.BaseDirectory, GetPlatformLibName()));
    }

    /// <summary>
    /// Gets the current version of the locally installed libmpv.
    /// On Windows, this reads the FileVersion from the DLL.
    /// </summary>
    /// <returns>The version string or null if not found.</returns>
    public async Task<string?> GetCurrentVersionAsync()
    {
        string libPath = Path.Combine(AppContext.BaseDirectory, GetPlatformLibName());
        if (!File.Exists(libPath))
            return null;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var info = FileVersionInfo.GetVersionInfo(libPath);
                return info.ProductVersion ?? info.FileVersion;
            }
            else if (
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            )
            {
                // On Unix platforms, we could try running 'mpv --version' if it's in the PATH,
                // but since we are managing the library specifically, we might look for a version file
                // or just leave it for now if we can't easily extract it from the .so/.dylib itself.
                // However, often 'mpv' command is available if the library is installed via package manager.
                return await GetUnixMpvVersionAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error getting libmpv version", ex);
        }

        return null;
    }

    private async Task<string?> GetUnixMpvVersionAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "mpv",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var firstLine = output
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(firstLine))
                return null;

            // Typically starts with "mpv 0.35.0-unknown ..."
            var match = Regex.Match(firstLine, @"mpv\s+([^\s]+)");
            return match.Success ? match.Groups[1].Value : firstLine;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the expected library filename for the current platform.
    /// </summary>
    private string GetPlatformLibName() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "libmpv-2.dll"
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "libmpv.dylib"
        : "libmpv.so";

    /// <summary>
    /// Event args for completion events raised after install/upgrade/uninstall operations.
    /// </summary>
    public sealed class InstallationCompletedEventArgs : EventArgs
    {
        public InstallationCompletedEventArgs(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; }
        public string Message { get; }
    }

    private static string? ExtractVersionFromText(string input)
    {
        if (string.IsNullOrEmpty(input))
            return null;
        var m = Regex.Match(input, @"\d+(\.\d+)+(-\w+)?");
        return m.Success ? m.Value : null;
    }
}
