using System;
using System.Text.Json.Serialization;
using Avalonia.Styling;
using Avayomi.Helpers;
using Cogwheel;
using SukiUI.Enums;

namespace Avayomi.Models;

public class Settings() : SettingsBase(
    EnvironmentHelper.ApplicationDataPath.JoinPath("settings.json"),
    GlobalJsonContext.Default)
{
    public int Id { get; set; }

    public SukiColor ThemeColor { get; set; } = SukiColor.Blue;

    public Theme Theme { get; set; } = Theme.Default;
    public bool BackgroundAnimation { get; set; } = true;
    public bool CheckForUpdates { get; set; } = true;
    public TimeSpan CheckForUpdatesInterval { get; set; } = TimeSpan.FromMinutes(5);
    public Suwayomi Suwayomi { get; set; } = new();

    [JsonIgnore] public ThemeVariant ThemeVariant => MapToThemeVariant(Theme);

    private static ThemeVariant MapToThemeVariant(Theme theme) =>
        theme switch
        {
            Theme.Dark => ThemeVariant.Dark,
            Theme.Light => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };
}