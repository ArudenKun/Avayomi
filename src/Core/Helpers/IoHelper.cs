using Core.Extensions;

namespace Core.Helpers;

public static class IoHelper
{
    public static void EnsureContainingDirectoryExists(string fileNameOrPath)
    {
        string fullPath = Path.GetFullPath(fileNameOrPath); // No matter if relative or absolute path is given to this.
        string? dir = Path.GetDirectoryName(fullPath);
        EnsureDirectoryExists(dir);
    }

    /// <summary>
    /// Makes sure that directory <paramref name="dir"/> is created if it does not exist.
    /// </summary>
    /// <remarks>Method does not throw exceptions unless provided directory path is invalid.</remarks>
    public static void EnsureDirectoryExists(string? dir)
    {
        // If root is given, then do not worry.
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public static void DeleteDirectory(string dirPath)
    {
        foreach (var folder in Directory.GetDirectories(dirPath))
        {
            DeleteDirectory(folder);
        }

        foreach (string file in Directory.GetFiles(dirPath))
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
        {
            if (blackList.Contains(output[i]))
            {
                output[i] = replacementChar;
            }
        }

        return new string(output);
    }

    // This method removes the path and file extension.
    //
    // Given Wasabi releases are currently built using Windows, the generated assemblies contain
    // the hard coded "C:\Users\User\Desktop\WalletWasabi\.......\FileName.cs" string because that
    // is the real path of the file, it doesn't matter what OS was targeted.
    // In Windows and Linux that string is a valid path and that means Path.GetFileNameWithoutExtension
    // can extract the file name but in the case of OSX the same string is not a valid path so, it assumes
    // the whole string is the file name.
    public static string ExtractFileName(string callerFilePath)
    {
        var lastSeparatorIndex = callerFilePath.LastIndexOf('\\');
        if (lastSeparatorIndex == -1)
        {
            lastSeparatorIndex = callerFilePath.LastIndexOf('/');
        }

        var fileName = callerFilePath;

        if (lastSeparatorIndex != -1)
        {
            lastSeparatorIndex++;
            fileName = callerFilePath[lastSeparatorIndex..]; // From lastSeparatorIndex until the end of the string.
        }

        var fileNameWithoutExtension = fileName.TrimEnd(
            ".cs",
            StringComparison.InvariantCultureIgnoreCase
        );
        return fileNameWithoutExtension;
    }
}
