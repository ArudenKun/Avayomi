using System;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Avayomi.Services.Settings;
using Avayomi.Settings;
using R3;
using Serilog.Core;
using Volo.Abp.DependencyInjection;

namespace Avayomi.Services;

[AutoExtractInterface]
public sealed class LoggingService : ILoggingService, IDisposable, ISingletonDependency
{
    private readonly LoggingLevelSwitch _loggingLevelSwitch;
    private readonly LoggingSettings _loggingSettings;

    private IDisposable? _subscription;

    public LoggingService(LoggingLevelSwitch loggingLevelSwitch, ISettingsService settingsService)
    {
        _loggingLevelSwitch = loggingLevelSwitch;
        _loggingSettings = settingsService.Get<LoggingSettings>();
    }

    public void Initialize()
    {
        _subscription = _loggingSettings
            .ObservePropertyChanged(x => x.LogEventLevel)
            .Subscribe(logEventLevel => _loggingLevelSwitch.MinimumLevel = logEventLevel);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
