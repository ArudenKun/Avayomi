using System;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using Avalonia.Styling;
using Avayomi.Styles;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using SukiUI.Enums;

namespace Avayomi.Settings;

public sealed partial class AppearanceSettings : ObservableObject
{
    [ObservableProperty]
    public partial Theme Theme { get; set; } = Theme.System;

    [JsonIgnore]
    public ThemeVariant ThemeVariant =>
        Theme switch
        {
            Theme.Light => ThemeVariant.Light,
            Theme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

    [ObservableProperty]
    public partial string ThemeColor { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool BackgroundAnimations { get; set; } = true;

    [ObservableProperty]
    public partial bool BackgroundTransitions { get; set; } = true;

    [ObservableProperty]
    public partial SukiBackgroundStyle BackgroundStyle { get; set; } =
        SukiBackgroundStyle.GradientSoft;

    [ObservableProperty]
    public partial WindowState LastWindowState { get; set; } = WindowState.FullScreen;

    [ObservableProperty]
    public partial TimeSpan ToastDuration { get; set; } = 5.Seconds();
}
