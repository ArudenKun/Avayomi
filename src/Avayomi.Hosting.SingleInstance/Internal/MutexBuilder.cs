using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Avayomi.Hosting.SingleInstance.Internal;

/// <summary>
/// Provides a builder for configuring and creating mutex instances used to coordinate application instance exclusivity.
/// </summary>
/// <remarks>This class is intended for internal use to facilitate the construction of mutexes that manage
/// single-instance application scenarios. It exposes properties for specifying the mutex identifier, scope, and actions
/// to perform when the current process is not the first instance.</remarks>
internal class MutexBuilder : IMutexBuilder
{
    /// <inheritdoc />
    public string? MutexId { get; set; }

    /// <inheritdoc />
    public bool IsGlobal { get; set; }

    /// <inheritdoc />
    public Action<IHostEnvironment, ILogger>? WhenNotFirstInstance { get; set; }
}
