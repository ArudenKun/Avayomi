using System.Collections.Generic;
using System.IO;

namespace Desktop.Helpers;

public static class IoHelper
{
    public static void EnsureContainingDirectoryExists(string fileNameOrPath)
    {
        var fullPath = Path.GetFullPath(fileNameOrPath); // No matter if relative or absolute path is given to this.
        var dir = Path.GetDirectoryName(fullPath);
        EnsureDirectoryExists(dir);
    }

    /// <summary>
    ///     Makes sure that directory <paramref name="dir" /> is created if it does not exist.
    /// </summary>
    /// <remarks>Method does not throw exceptions unless provided directory path is invalid.</remarks>
    public static void EnsureDirectoryExists(string? dir)
    {
        // If root is given, then do not worry.
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public static void DeleteDirectory(string dirPath)
    {
        foreach (var folder in Directory.GetDirectories(dirPath))
            DeleteDirectory(folder);

        foreach (var file in Directory.GetFiles(dirPath))
        {
            var pPath = Path.Combine(dirPath, file);
            File.SetAttributes(pPath, FileAttributes.Normal);
            File.Delete(file);
        }

        Directory.Delete(dirPath);
    }

    public static string SanitizeFileName(this string source, char replacementChar = '_')
    {
        var blackList = new HashSet<char>(Path.GetInvalidFileNameChars()) { '"' }; // '"' not invalid in Linux, but causes problems
        var output = source.ToCharArray();
        for (int i = 0, ln = output.Length; i < ln; i++)
            if (blackList.Contains(output[i]))
                output[i] = replacementChar;

        return new string(output);
    }
}
