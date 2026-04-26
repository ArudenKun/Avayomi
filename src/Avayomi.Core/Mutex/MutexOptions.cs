using System.Reflection;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Avayomi.Core.Mutex;

/// <summary>
/// Provides a builder for configuring and creating mutex instances used to coordinate application instance exclusivity.
/// </summary>
/// <remarks>This class is intended for internal use to facilitate the construction of mutexes that manage
/// single-instance application scenarios. It exposes properties for specifying the mutex identifier, scope, and actions
/// to perform when the current process is not the first instance.</remarks>
public class MutexOptions
{
    public string BasePath { get; set; } = AvayomiCoreConsts.Paths.DataDir;

    /// <summary>
    /// Gets or Sets whether to use a FileLock instead of a ResourceLock
    /// </summary>
    public bool UseFileLock { get; set; } = false;

    public string ApplicationName { get; set; } =
        Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for the mutex associated with the current operation or resource.
    /// </summary>
    public string MutexId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the setting applies globally to all users or contexts.
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// Gets or sets the action to execute when the current process is not the first instance of the application.
    /// </summary>
    /// <remarks>This action is invoked with the application's host environment and logger as parameters. Use
    /// this property to define custom behavior, such as notifying the user or logging a message, when a subsequent
    /// instance of the application is detected.</remarks>
    public Action<IAbpHostEnvironment, ILogger>? WhenNotFirstInstance { get; set; }
}
