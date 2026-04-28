using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using PleasantUI;
using Volo.Abp.DependencyInjection;

namespace Avayomi.ViewModels.Pages;

[Dependency(ServiceLifetime.Singleton)]
public sealed class SettingsPageViewModel : PageViewModel
{
    public SettingsPageViewModel()
    {
        IsVisibleOnSideMenu = false;
    }

    public override int Index => int.MaxValue;
    public override Geometry IconKind => MaterialIcons.Cog;
    public override bool IsTopLevel => false;
}
