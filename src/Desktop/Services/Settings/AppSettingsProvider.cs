using System.Text.Json.Serialization.Metadata;
using Desktop.Helpers;
using Desktop.Models;
using Microsoft.Extensions.Logging;

namespace Desktop.Services.Settings;

public class AppSettingsProvider : BaseSettingsProvider<AppSettings>
{
    public AppSettingsProvider(
        ILogger<AppSettingsProvider> logger,
        IJsonTypeInfoResolver jsonTypeInfoResolver
    )
        : base(logger, jsonTypeInfoResolver) { }

    public override string FilePath =>
        EnvironmentHelper.ApplicationDataPath.JoinPath("settings.json");
}
