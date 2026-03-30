using System.Threading.Tasks;
using Avayomi.Services.Dialogs;
using Avayomi.ViewModels.Dialogs;
using Avayomi.Views;
using CommunityToolkit.Mvvm.Input;
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
    private async Task ShowSettingsAsync()
    {
        await DialogService.ShowDialogAsync(
            ServiceProvider.GetRequiredService<SettingsDialogViewModel>()
        );

        // Messenger.Send(new ChangePageMessage(typeof(SettingsPageViewModel)));
    }
}
