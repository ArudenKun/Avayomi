using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.ViewModels.Common;
using Material.Icons;
using ZiggyCreatures.Caching.Fusion;

namespace Desktop.ViewModels.Pages;

public partial class BrowsePageViewModel : BasePageViewModel
{
    [ObservableProperty]
    private bool _loadingState;

    private readonly IFusionCache _fusionCache;

    public BrowsePageViewModel(IFusionCache fusionCache)
    {
        _fusionCache = fusionCache;
    }

    public override int Index => 3;
    public override MaterialIconKind Icon => MaterialIconKind.Compass;

    [RelayCommand]
    private async Task ChangeLoadingState()
    {
        LoadingState = !LoadingState;
        await _fusionCache.GetOrSetAsync("key:1", Random.Shared.Next(1, 100_000));
    }
}
