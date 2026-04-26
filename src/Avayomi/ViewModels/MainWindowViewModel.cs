using System;
using Avayomi.Extensions;
using Avayomi.Messaging.Messages;
using Avayomi.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NavigationEventArgs = AsyncNavigation.Core.NavigationEventArgs;

namespace Avayomi.ViewModels;

public sealed partial class MainWindowViewModel : ViewModel
{
    public override void OnLoaded()
    {
        if (RegionManager.TryGetRegion(Regions.Main, out var mainRegion))
        {
            mainRegion.Navigated += NavigationHostManagerOnHostChanged;
        }

        RegionManager.RequestNavigate<ShellView>(Regions.Main);
    }

    private void NavigationHostManagerOnHostChanged(object? sender, NavigationEventArgs e)
    {
        IsMainView = e.Context.ViewName == typeof(MainView).ViewName;
    }

    [ObservableProperty]
    public partial bool IsMainView { get; set; }

    [RelayCommand]
    private void ChangePage(Type pageViewMoelType)
    {
        Messenger.Send(new ChangePageMessage(pageViewMoelType));
    }
}
