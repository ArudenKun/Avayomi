using System.Runtime.InteropServices;
using Avayomi.Core.Extensions;
using Volo.Abp.IO;

namespace Avayomi.Core;

public static class AvayomiCoreConsts
{
    public static bool IsDebug { get; set; }
#if DEBUG
        = true;
#else
        = false;
#endif
    public static string Name { get; set; } = "Avayomi";

    public static class Paths
    {
        public static string AppDir { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        public static string ContentRootDir { get; set; } =
            AppContext.BaseDirectory[
                ..AppContext.BaseDirectory.LastIndexOf("bin", StringComparison.OrdinalIgnoreCase)
            ];

        public static string RoamingDir { get; set; } =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public static string DataDir
        {
            get
            {
                if (
                    !File.Exists(AppDir.Combine(".portable"))
                    && !Directory.Exists(AppDir.Combine("data"))
                    && !IsDebug
                )
                    return RoamingDir.Combine(Name);
                var dataDir = AppDir.Combine("data");
                DirectoryHelper.CreateIfNotExists(dataDir);
                return dataDir;
            }
        }

        public static string CacheDir { get; set; } = DataDir.Combine("Cache");
        public static string LogsDir { get; set; } = DataDir.Combine("Logs");
        public static string SettingsPath { get; set; } = DataDir.Combine("settings.json");
    }

    public static class OperatingSystem
    {
        public static bool IsArm =
            RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            || RuntimeInformation.ProcessArchitecture == Architecture.Arm;

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static string Name { get; } =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macos"
            : "linux";
    }
}
