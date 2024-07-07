using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.ViewModels.Common;
using Material.Icons;

namespace Desktop.ViewModels.Pages;

public partial class BrowsePageViewModel : BasePageViewModel
{
    [ObservableProperty]
    private bool _loadingState;

    public override int Index => 3;
    public override MaterialIconKind Icon => MaterialIconKind.Compass;

    [RelayCommand]
    private void ChangeLoadingState()
    {
        LoadingState = !LoadingState;
    }
}
