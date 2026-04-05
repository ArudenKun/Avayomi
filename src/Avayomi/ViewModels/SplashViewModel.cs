using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Avayomi.Navigation;
using Avayomi.Services;
using Avayomi.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace Avayomi.ViewModels;

[Dependency(ServiceLifetime.Singleton)]
public sealed partial class SplashViewModel : ViewModel, INavigationAware
{
    private readonly IAniListService _aniListService;

    public SplashViewModel(IAniListService aniListService)
    {
        _aniListService = aniListService;
    }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Initializing";

    public override void OnLoaded()
    {
        StartAsync().SafeFireAndForget();
    }

    private async Task StartAsync()
    {
        await Task.Delay(1000);
        StatusText = "Checking login cache";
        await _aniListService.CheckAuthenticationCacheAsync();
        if (_aniListService.IsAuthenticated)
        {
            await NavigationHostManager.NavigateAsync<MainView>(
                HostNames.Main,
                await _aniListService.GetAuthenticatedUserAsync()
            );
            return;
        }

        await NavigationHostManager.NavigateAsync<LoginView>(HostNames.Main);

        // if (GeneralOptions.ShowConsole)
        // {
        //     // Messenger.Send(new ConsoleWindowShowMessage());
        // }
        //
        //await Task.Delay(1.Seconds());
        //StatusText = "Loading Settings";
        //await Task.Delay(200.Milliseconds());
        //var message = GeneralSettings.IsSetup
        //    ? new SplashFinishedMessage(typeof(SetupView))
        //    : new SplashFinishedMessage(typeof(MainView));
        //Messenger.Send(message);
    }

    public bool CanNavigateTo(object? parameter)
    {
        return true;
    }

    public void OnNavigatedTo(object? parameter) { }

    public bool CanNavigateFrom()
    {
        return true;
    }

    public void OnNavigatedFrom() { }
}
