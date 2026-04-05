using System;
using Avayomi.Messaging.Messages;
using Avayomi.Navigation;
using Avayomi.Views;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Volo.Abp.DependencyInjection;

namespace Avayomi.ViewModels;

[Dependency(ServiceLifetime.Singleton)]
public sealed partial class MainWindowViewModel : ViewModel
{
    public required ISukiDialogManager DialogManager { get; init; }
    public required ISukiToastManager ToastManager { get; init; }

    public override void OnLoaded()
    {
        base.OnLoaded();
        NavigationHostManager.HostChanged += NavigationHostManagerOnHostChanged;
        NavigationHostManager.Navigate<SplashView>(HostNames.Main);
    }

    public override void OnUnloaded()
    {
        base.OnUnloaded();
        NavigationHostManager.HostChanged -= NavigationHostManagerOnHostChanged;
    }

    private void NavigationHostManagerOnHostChanged(
        object? sender,
        NavigationHostChangedEventArgs e
    )
    {
        OnPropertyChanged(nameof(IsMainView));
    }

    public bool IsMainView =>
        NavigationHostManager.GetHost(HostNames.Main)?.CurrentContent is MainView;

    [RelayCommand]
    private void ChangePage(Type pageViewMoelType)
    {
        Messenger.Send(new ChangePageMessage(pageViewMoelType));
    }
}
