using System.Runtime.InteropServices;
using Avayomi.Core.Extensions;
using Volo.Abp.IO;

namespace Avayomi.Core;

public static class AvayomiCoreConsts
{
    public const bool IsDebug
#if DEBUG
    = true;
#else
    = false;
#endif
    public const string Name = "Avayomi";

    public static class Paths
    {
        public static string AppDir { get; } = AppDomain.CurrentDomain.BaseDirectory;

        public static string ContentRootDir { get; } =
            AppContext.BaseDirectory[
                ..AppContext.BaseDirectory.LastIndexOf("bin", StringComparison.OrdinalIgnoreCase)
            ];

        public static string RoamingDir { get; } =
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
#pragma warning disable CS0162 // Unreachable code detected
                    // ReSharper disable once HeuristicUnreachableCode
                    return RoamingDir.Combine(Name);
#pragma warning restore CS0162 // Unreachable code detected
                var dataDir = AppDir.Combine("data");
                DirectoryHelper.CreateIfNotExists(dataDir);
                return dataDir;
            }
        }

        public static string CacheDir { get; } = DataDir.Combine("Cache");
        public static string LogsDir { get; } = DataDir.Combine("Logs");

        public static string ToolsDir { get; } = DataDir.Combine("Tools");
        public static string SettingsPath { get; } = DataDir.Combine("settings.json");
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
