using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Avayomi.Helpers;

public static class OsHelper
{
    public static bool IsWindows => GetOSVersion() == OSVersion.Windows;
    public static bool IsLinux => GetOSVersion() == OSVersion.Linux;

    /// <summary>
    /// Gets the <see cref="OSVersion"/> depending on what platform you are on
    /// </summary>
    /// <returns>Returns the OS Version</returns>
    /// <exception cref="Exception"></exception>
    // ReSharper disable once InconsistentNaming
    public static OSVersion GetOSVersion()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OSVersion.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OSVersion.Linux;
        }

        throw new Exception("Your OS isn't supported");
    }
}

// ReSharper disable once InconsistentNaming
public enum OSVersion
{
    [Description("win-x64")]
    Windows,

    [Description("linux-x64")]
    Linux
}
