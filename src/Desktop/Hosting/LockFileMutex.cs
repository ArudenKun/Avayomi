using System;
using System.IO;
using Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Desktop.Hosting;

/// <summary>
///     This protects your application from running more than once
/// </summary>
public sealed class LockFileMutex : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _id;
    private readonly string _name;
    private readonly string _fileName;
    private FileStream? _stream;

    /// <summary>
    ///     Private constructor
    /// </summary>
    /// <param name="logger">ILogger</param>
    /// <param name="id">string with a unique Mutex ID</param>
    /// <param name="name">optional name for the resource</param>
    private LockFileMutex(ILogger logger, string id, string name)
    {
        _logger = logger;
        _id = id;
        _name = name;
        _fileName = Path.GetTempPath().JoinPath(_name + ".lock");
    }

    /// <summary>
    ///     Test if the Mutex was created and locked.
    /// </summary>
    public bool IsLocked { get; private set; }

    /// <summary>
    ///     Create a ResourceMutex for the specified mutex id and resource-name
    /// </summary>
    /// <param name="logger">ILogger</param>
    /// <param name="id">ID of the mutex, preferably a Guid as string</param>
    /// <param name="name">Name of the resource to lock, e.g your application name, useful for logs</param>
    public static LockFileMutex Create(ILogger? logger, string id, string name)
    {
        logger ??= NullLoggerFactory.Instance.CreateLogger<LockFileMutex>();
        var lockFileMutex = new LockFileMutex(logger, id, name);
        lockFileMutex.Lock();
        return lockFileMutex;
    }

    /// <summary>
    ///     This tries to get the Lock, which takes care of having multiple instances running
    /// </summary>
    /// <returns>true if it worked, false if another instance is already running or something went wrong</returns>
    public bool Lock()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("{0} is trying to get Lock {1}", _name, _id);
        }

        IsLocked = true;
        try
        {
            _stream = new FileStream(
                _fileName,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None
            );

            _logger.LogInformation("{0} has created and claimed the Lock {1}", _name, _id);
        }
        catch (IOException ex) when (ex.Message.Contains(_fileName))
        {
            _logger.LogWarning(
                "Lock {0} is already in use and couldn't be locked for the caller {1}",
                _id,
                _name
            );
            Dispose(false);
            IsLocked = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Problem obtaining the Lock {Id} for {Name}, assuming it was already taken!",
                _id,
                _name
            );
            IsLocked = false;
        }

        return IsLocked;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        if (_stream == null)
        {
            return;
        }

        try
        {
            if (!IsLocked)
                return;

            _stream.Dispose();
            _stream = null;

            _logger.LogInformation("Released Lock {0} for {1}", _id, _name);

            try
            {
                File.Delete(_name);
                _logger.LogInformation("Deleted Lock {0} for {1}", _id, _name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Lock {0} for {1}", _id, _name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing Lock {0} for {1}", _id, _name);
        }
    }

    ~LockFileMutex()
    {
        Dispose(false);
    }
}
