using AsyncNavigation.Abstractions;
using Avalonia.Controls;
using Avalonia.Markup.Declarative;
using Avayomi.Extensions;

namespace Avayomi.Views;

public sealed class ShellView : ViewBase
{
    private readonly IRegionManager _regionManager;

    public ShellView(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    protected override object Build() =>
        new ContentControl()
            .RegionManager_RegionName(Regions.Main)
            .OnLoaded(_ => _regionManager.RequestNavigate<SplashView>(Regions.Main));
}
