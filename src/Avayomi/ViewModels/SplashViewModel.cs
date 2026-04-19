using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using AsyncNavigation;
using AsyncNavigation.Core;
using Avayomi.Services;
using Avayomi.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace Avayomi.ViewModels;

public sealed partial class SplashViewModel : ViewModel
{
    private readonly IAniListService _aniListService;

    public SplashViewModel(IAniListService aniListService)
    {
        _aniListService = aniListService;
    }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Initializing";

    public override void OnLoaded() { }

    public override Task OnNavigatedToAsync(NavigationContext context)
    {
        // base.OnNavigatedToAsync(context);
        // StartAsync().SafeFireAndForget();
        return Task.CompletedTask;
    }

    private async Task StartAsync()
    {
        await Task.Delay(1000);
        StatusText = "Checking login cache";
        await _aniListService.CheckAuthenticationCacheAsync();
        if (_aniListService.IsAuthenticated)
        {
            await RegionManager.RequestNavigateAsync(
                Regions.Main,
                Regions.Main,
                new NavigationParameters
                {
                    { "Auth", await _aniListService.GetAuthenticatedUserAsync() },
                }
            );
            return;
        }

        await RegionManager.RequestNavigateAsync(Regions.Main, Regions.Main);

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
}
