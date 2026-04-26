using System.Threading.Tasks;
using AsyncNavigation;
using AsyncNavigation.Core;
using Avayomi.Core.AniList.Models.User;
using Avayomi.Extensions;
using Avayomi.Services;
using Avayomi.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PleasantUI.Controls;

namespace Avayomi.ViewModels;

public sealed partial class MainViewModel : ViewModel
{
    private readonly IAniListService _aniListService;

    public MainViewModel(IAniListService aniListService)
    {
        _aniListService = aniListService;
    }

    [ObservableProperty]
    public partial NavigationViewItem? NavigationViewItem { get; set; }

    [ObservableProperty]
    public partial User? User { get; set; }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _aniListService.LogoutAsync();
        await RegionManager.RequestNavigateAsync<LoginView>(
            Regions.Main,
            new NavigationParameters { { "IsLogout", true } }
        );
    }

    partial void OnNavigationViewItemChanged(
        NavigationViewItem? oldValue,
        NavigationViewItem? newValue
    )
    {
        oldValue?.IsSelected = false;
    }

    //public void Receive(ChangePageMessage message)
    //{
    //    var item = Pages.FirstOrDefault(x => x.Tag as Type == message.ViewModelType);
    //    if (item is null)
    //    {
    //        ToastService.ShowToast(
    //            NotificationType.Error,
    //            "Navigation Error",
    //            "An internal error occurred while navigating to the page."
    //        );
    //        return;
    //    }

    //    // Setting PageItem automatically triggers OnPageItemChanged -> ChangePage
    //    Page = item;
    //}

    //partial void OnPageChanged(SukiSideMenuItem? value)
    //{
    //    if (value?.Tag is not Type viewModelType)
    //        return;
    //    Logger.LogInformation("PageItemChange {Header}", value.Header);
    //    ChangeContent(viewModelType);
    //}

    public override async Task OnNavigatedToAsync(NavigationContext context)
    {
        await base.OnNavigatedToAsync(context);

        // User? user = null;
        //
        // if (
        //     context.Parameters is { } parameters
        //     && parameters.TryGetValue<User>("Auth", out var navUser)
        // )
        // {
        //     user = navUser;
        // }
        //
        // user ??= await _aniListService.GetAuthenticatedUserAsync();
        // User = user;
    }
}
