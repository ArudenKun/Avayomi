using CommunityToolkit.Mvvm.ComponentModel;

namespace Avayomi.Settings;

public sealed partial class GeneralSettings : ObservableObject
{
    [ObservableProperty]
    public partial bool AutoUpdate { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowConsole { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSetup { get; set; } = true;

    [ObservableProperty]
    public partial string Provider { get; set; } = string.Empty;
}
