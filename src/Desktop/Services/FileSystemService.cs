using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Ardalis.GuardClauses;
using AutoInterfaceAttributes;
using Desktop.Extensions;

namespace Desktop.Services;

/// <summary>
/// Extends IFileSystem with a few extra IO functions. IFileSystem provides wrappers around all IO methods.
/// </summary>
[AutoInterface(Inheritance = [typeof(IFileSystem)])]
public class FileSystemService : IFileSystemService
{
    private readonly IFileSystem _fileSystem;

    public FileSystemService(IFileSystem fileSystemService)
    {
        _fileSystem = fileSystemService;
    }

    public IDirectory Directory => _fileSystem.Directory;
    public IDirectoryInfoFactory DirectoryInfo => _fileSystem.DirectoryInfo;
    public IDriveInfoFactory DriveInfo => _fileSystem.DriveInfo;
    public IFile File => _fileSystem.File;
    public IFileInfoFactory FileInfo => _fileSystem.FileInfo;
    public IFileStreamFactory FileStream => _fileSystem.FileStream;
    public IFileSystemWatcherFactory FileSystemWatcher => _fileSystem.FileSystemWatcher;
    public IPath Path => _fileSystem.Path;

    /// <summary>
    /// Ensures the directory of specified path exists. If it doesn't exist, creates the directory.
    /// </summary>
    /// <param name="path">The absolute path to validate.</param>
    /// <inheritdoc />
    public virtual void EnsureDirectoryExists(string path)
    {
        if (_fileSystem.Path.IsPathRooted(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
        }
    }

    /// <summary>
    /// Deletes a file if it exists.
    /// </summary>
    /// <param name="path">The path of the file to delete.</param>
    public virtual void DeleteFileSilent(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException) { }
    }

    /// <summary>
    /// Returns all files of specified extensions.
    /// </summary>
    /// <param name="path">The path in which to search.</param>
    /// <param name="extensions">A list of file extensions to return, each extension must include the dot.</param>
    /// <param name="searchOption">Specifies additional search options.</param>
    /// <returns>A list of files paths matching search conditions.</returns>
    public virtual IEnumerable<string> GetFilesByExtensions(
        string path,
        IEnumerable<string> extensions,
        SearchOption searchOption = SearchOption.TopDirectoryOnly
    )
    {
        Guard.Against.NullOrWhiteSpace(path);

        try
        {
            return Directory
                .EnumerateFiles(path, "*", searchOption)
                .Where(f =>
                    extensions.Any(s => f.EndsWith(s, StringComparison.InvariantCultureIgnoreCase))
                );
        }
        catch (DirectoryNotFoundException) { }
        catch (UnauthorizedAccessException) { }
        catch (PathTooLongException) { }

        return Array.Empty<string>();
    }

    /// <summary>
    /// Returns specified path without its file extension.
    /// </summary>
    /// <param name="path">The path to truncate extension from.</param>
    /// <returns>A file path with no file extension.</returns>
    public virtual string GetPathWithoutExtension(string path)
    {
        Guard.Against.NullOrWhiteSpace(path);
        return Path.Combine(
            Path.GetDirectoryName(path) ?? "",
            Path.GetFileNameWithoutExtension(path)
        );
    }

    /// <summary>
    /// Returns the path ensuring it ends with a directory separator char.
    /// </summary>
    /// <param name="path">The path to end with a separator char.</param>
    /// <returns>A path that must end with a directory separator char.</returns>
    public virtual string GetPathWithFinalSeparator(string path)
    {
        Guard.Against.NullOrWhiteSpace(path);
        if (!path.EndsWith(Path.DirectorySeparatorChar))
        {
            path += Path.DirectorySeparatorChar;
        }

        return path;
    }

    /// <summary>
    /// Replaces all illegal chars in specified file name with specified replacement character.
    /// </summary>
    /// <param name="fileName">The file name to sanitize.</param>
    /// <param name="replacementChar">The replacement character.</param>
    /// <returns></returns>
    public virtual string SanitizeFileName(string fileName, char replacementChar = '_')
    {
        var blackList = new HashSet<char>(Path.GetInvalidFileNameChars()) { '"' }; // '"' not invalid in Linux, but causes problems
        var output = fileName.ToCharArray();
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
            lastSeparatorIndex = callerFilePath.LastIndexOf('/');

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
