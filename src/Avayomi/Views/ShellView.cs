using AsyncNavigation.Abstractions;
using Avayomi.Extensions;
using Avayomi.ViewModels;

namespace Avayomi.Views;

public sealed class ShellView : View<ShellViewModel>
{
    private readonly IRegionManager _regionManager;

    public ShellView(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    protected override object Build(ShellViewModel vm) =>
        new ContentControl()
            .RegionManager_RegionName(Regions.Main)
            .OnLoaded(_ => _regionManager.RequestNavigateAsync<MainView>(Regions.Main));
}
