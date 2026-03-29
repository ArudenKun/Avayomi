using Lucide.Avalonia;

namespace Avayomi.ViewModels.Pages;

public sealed partial class SettingsPageViewModel : PageViewModel
{
    public SettingsPageViewModel()
    {
        IsVisibleOnSideMenu = false;
    }

    public override int Index => int.MaxValue;
    public override LucideIconKind IconKind => LucideIconKind.Settings;
}
