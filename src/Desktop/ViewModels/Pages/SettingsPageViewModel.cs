using Desktop.ViewModels.Common;
using Material.Icons;

namespace Desktop.ViewModels.Pages;

public class SettingsPageViewModel : BasePageViewModel
{
    public override int Index => 999;
    public override MaterialIconKind Icon => MaterialIconKind.Settings;
}
