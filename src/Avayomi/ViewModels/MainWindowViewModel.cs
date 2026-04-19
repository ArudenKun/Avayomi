using System;
using System.Threading.Tasks;
using AsyncNavigation;
using AsyncNavigation.Core;
using Avayomi.Messaging.Messages;
using Avayomi.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace Avayomi.ViewModels;

public sealed partial class MainWindowViewModel : ViewModel
{
    public required ISukiDialogManager DialogManager { get; init; }
    public required ISukiToastManager ToastManager { get; init; }

    public override void OnLoaded()
    {
        if (RegionManager.TryGetRegion(Regions.Main, out var mainRegion))
        {
            mainRegion.Navigated += NavigationHostManagerOnHostChanged;
        }

        _ = RegionManager.RequestNavigateAsync(Regions.Main, SplashView.ViewName, null, true);
    }

    private void NavigationHostManagerOnHostChanged(object? sender, NavigationEventArgs e)
    {
        IsMainView = e.Context.ViewName == MainView.ViewName;
    }

    [ObservableProperty]
    public partial bool IsMainView { get; set; }

    [RelayCommand]
    private void ChangePage(Type pageViewMoelType)
    {
        Messenger.Send(new ChangePageMessage(pageViewMoelType));
    }
}
