using System;
using Avayomi.Messaging.Messages;
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
        NavigationHostManager.Navigate<MainView>(HostNames.Main);
    }

    [RelayCommand]
    private void ChangePage(Type pageViewMoelType)
    {
        Messenger.Send(new ChangePageMessage(pageViewMoelType));
    }
}
