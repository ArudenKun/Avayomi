using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avayomi.Settings;

public sealed partial class AppearanceSettings : ObservableObject
{
    [ObservableProperty]
    public partial string ThemeColor { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool BackgroundAnimations { get; set; } = true;

    [ObservableProperty]
    public partial bool BackgroundTransitions { get; set; } = true;

    [ObservableProperty]
    public partial WindowState LastWindowState { get; set; } = WindowState.FullScreen;
}
