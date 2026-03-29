using System.Text.Json;
using Avayomi.Core;

namespace Avayomi.Services.Settings;

public class SettingsServiceOptions
{
    public string FilePath { get; set; } = AvayomiCoreConsts.Paths.SettingsPath;

    public JsonSerializerOptions JsonSerializerOptions { get; set; } =
        JsonSerializerOptions.Default;
}
