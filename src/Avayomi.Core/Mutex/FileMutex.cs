using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Avayomi.Core.Mutex;

/// <summary>
/// Provides a cross-process lock utilizing a physical file on disk for synchronizing access to a resource, ensuring
/// that only one instance can hold the lock at a time.
/// </summary>
/// <remarks>Use this class to prevent multiple instances of an application or process from accessing the same
/// resource simultaneously. The class is disposable; always call Dispose when the lock is no longer needed to
/// release the file handle and associated resources.</remarks>
public sealed class FileLockMutex : IMutex
{
    private readonly ILogger _logger;
    private readonly string _lockFilePath;
    private readonly string _resourceName;
    private FileStream? _lockFileStream;
    private bool _disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLockMutex"/> class with the specified logger, file path, and optional resource name.
    /// </summary>
    /// <param name="logger">The logger instance used to record diagnostic and operational messages.</param>
    /// <param name="lockFilePath">The full path to the file used for locking.</param>
    /// <param name="resourceName">The name of the resource associated with the lock. If null, the file name is used.</param>
    private FileLockMutex(ILogger logger, string lockFilePath, string? resourceName = null)
    {
        _logger = logger;
        _lockFilePath = lockFilePath;
        _resourceName = resourceName ?? Path.GetFileName(lockFilePath);
    }

    /// <summary>
    /// Gets a value indicating whether the object is locked.
    /// </summary>
    public bool IsLocked { get; private set; }

    /// <summary>
    /// Creates and attempts to acquire a new file lock for synchronizing access to a resource.
    /// </summary>
    /// <param name="logger">The logger to use for diagnostic messages. If null, a default logger is created.</param>
    /// <param name="lockFilePath">The file path used to establish the lock. Cannot be null or whitespace.</param>
    /// <param name="resourceName">An optional name for the resource being protected.</param>
    /// <returns>A FileLockMutex instance that is already acquired (if available) and can be used to synchronize access.</returns>
    /// <exception cref="ArgumentNullException">Thrown if lockFilePath is null, empty, or consists only of white-space characters.</exception>
    public static FileLockMutex Create(
        ILogger? logger,
        string? lockFilePath,
        string? resourceName = null
    )
    {
        if (string.IsNullOrWhiteSpace(lockFilePath))
        {
            throw new ArgumentNullException(nameof(lockFilePath));
        }

        logger ??= NullLogger.Instance;
        var fileLock = new FileLockMutex(logger, lockFilePath, resourceName);
        fileLock.Lock();
        return fileLock;
    }

    public bool Lock()
    {
        return Lock(TimeSpan.FromSeconds(2)); // Match the 2000ms timeout from the original implementation
    }

    /// <summary>
    /// Attempts to acquire an exclusive file lock.
    /// </summary>
    /// <remarks>Unlike System.Threading.Mutex, FileStreams do not require thread affinity for disposal.
    /// Any thread can call <see cref="Dispose"/> to release the lock.</remarks>
    /// <param name="timeout">The maximum amount of time to wait for the lock. Defaults to 2 seconds.</param>
    /// <returns>true if the lock was successfully acquired; otherwise, false.</returns>
    public bool Lock(TimeSpan timeout)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "{resourceName} is trying to get File Lock {lockFilePath}",
                _resourceName,
                _lockFilePath
            );
        }

        // Ensure the directory exists before trying to create the file
        var directory = Path.GetDirectoryName(_lockFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            try
            {
                // FileShare.None is the critical parameter: it requests exclusive access to the file.
                // FileOptions.DeleteOnClose ensures the lock file is cleaned up from disk once we are done.
                _lockFileStream = new FileStream(
                    _lockFilePath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.DeleteOnClose
                );

                IsLocked = true;
                _logger.LogInformation(
                    "{resourceName} has claimed the file lock {lockFilePath}",
                    _resourceName,
                    _lockFilePath
                );
                return true;
            }
            catch (IOException)
            {
                // IOException is thrown when the file is locked by another process (FileShare.None)
                if (stopwatch.Elapsed > timeout)
                {
                    _logger.LogWarning(
                        "File lock {lockFilePath} is already in use and couldn't be locked for the caller {resourceName}",
                        _lockFilePath,
                        _resourceName
                    );
                    IsLocked = false;
                    return false;
                }

                // Wait a short duration before retrying to prevent CPU thrashing
                Thread.Sleep(50);
            }
            catch (UnauthorizedAccessException e)
            {
                _logger.LogError(
                    e,
                    "{resourceName} lacks permissions to create/get file lock {lockFilePath}.",
                    _resourceName,
                    _lockFilePath
                );
                IsLocked = false;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Problem obtaining the File Lock {lockFilePath} for {resourceName}, assuming it was already taken!",
                    _resourceName,
                    _lockFilePath
                );
                IsLocked = false;
                return false;
            }
        }
    }

    /// <summary>
    /// Releases the file handle, which releases the exclusive lock on the file.
    /// </summary>
    public void Dispose()
    {
        if (_disposedValue)
        {
            return;
        }

        _disposedValue = true;

        try
        {
            if (IsLocked && _lockFileStream != null)
            {
                _lockFileStream.Dispose();
                IsLocked = false;
                _logger.LogInformation(
                    "Released File Lock {lockFilePath} for {resourceName}",
                    _lockFilePath,
                    _resourceName
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error releasing File Lock {lockFilePath} for {resourceName}",
                _lockFilePath,
                _resourceName
            );
        }
    }
}
