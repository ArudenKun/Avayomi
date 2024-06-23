using Avayomi.ViewModels.Abstractions;
using Material.Icons;

namespace Avayomi.ViewModels.Pages;

public class SettingsPageViewModel : BasePageViewModel
{
    public override int Index => 999;
    public override MaterialIconKind Icon => MaterialIconKind.Settings;
}
