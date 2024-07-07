using System;
using Avalonia;

namespace Desktop.Hosting;

public sealed record AvayomiAppOptions(
    string[] Args,
    string MutexId = "C9B35343-E41D-4B2C-A8F3-ADF7BC93D991",
    string? MutexName = null,
    Action<AppBuilder>? ConfigureAppBuilderDelegate = null
);
