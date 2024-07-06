using System;
using Avalonia;

namespace Desktop.Hosting;

public record AvayomiAppOptions(string[] Args, Action<AppBuilder>? ConfigureAppBuilderDelegate = null);