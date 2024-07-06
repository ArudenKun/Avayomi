using System.Diagnostics;

namespace Core.Helpers;

public static class PathHelper
{
    public static string JoinPath(this string path, params string[] parts)
    {
        var paths = new List<string> { path };
        paths.AddRange(parts);
        return Path.Combine([.. paths]);
    }

    /// <summary>
    /// Returns the absolute path for the specified path string.
    /// Also searches the environment's PATH variable.
    /// </summary>
    /// <param name="fileName">The relative path string.</param>
    /// <returns>The absolute path or null if the file was not found.</returns>
    public static string? GetFullPath(string fileName)
    {
        if (File.Exists(fileName))
            return Path.GetFullPath(fileName);

        var values = Environment.GetEnvironmentVariable("PATH");

        return values
            ?.Split(Path.PathSeparator)
            .Select(p => Path.Combine(p, fileName))
            .FirstOrDefault(File.Exists);
    }

    public static string GetParent(int levels, string currentPath)
    {
        var path = "";
        for (int i = 0; i < levels; i++)
        {
            path += $"..{Path.DirectorySeparatorChar}";
        }

        return Path.GetFullPath(Path.Combine(currentPath, path));
    }

    /// <summary>
    /// Gets the path of the command executable.
    /// </summary>
    /// <param name="cmd"></param>
    /// <returns></returns>
    public static string? GetFromEnvironment(string cmd)
    {
        string? result = null;
        string whichCommand = OperatingSystem.IsWindows() ? "where" : "which";

        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = whichCommand,
            Arguments = cmd,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        process.EnableRaisingEvents = true;
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                result = args.Data;
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return result;
    }
}
