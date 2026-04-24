using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncNavigation;
using AsyncNavigation.Core;
using Avalonia.Collections;
using Avalonia.Controls;
using Avayomi.Core.AniList.Models.User;
using Avayomi.Extensions;
using Avayomi.Services;
using Avayomi.ViewModels.Pages;
using Avayomi.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServiceScan.SourceGenerator;
using ZLinq;

namespace Avayomi.ViewModels;

public sealed partial class MainViewModel : ViewModel //, IRecipient<ChangePageMessage>
{
    private readonly IAniListService _aniListService;

    public MainViewModel(IEnumerable<PageViewModel> pageViewModels, IAniListService aniListService)
    {
        _aniListService = aniListService;

        var orderedPageViewModels = pageViewModels
            .AsValueEnumerable()
            .OrderBy(x => x.Index)
            .ToList();

        Pages.AddRange(orderedPageViewModels);
    }

    [ObservableProperty]
    public partial PageViewModel? Page { get; set; }

    public IAvaloniaList<PageViewModel> Pages { get; } = new AvaloniaList<PageViewModel>();

    [ObservableProperty]
    public partial User? User { get; set; }

    public override void OnLoaded()
    {
        base.OnLoaded();

        // Assigning PageItem instead of calling ChangePage directly.
        // This keeps the UI selection visually in sync with the current page.
        Page = Pages.FirstOrDefault(x => x.IsVisibleOnSideMenu);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _aniListService.LogoutAsync();
        await RegionManager.RequestNavigateAsync<LoginView>(
            Regions.Main,
            new NavigationParameters { { "IsLogout", true } }
        );
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

    private void ChangeContent(Type viewModelType)
    {
        var itemDefinition = GetSideMenuItemDefinitions()
            .FirstOrDefault(x => x.ViewModelType == viewModelType);
        if (itemDefinition is null)
            return;
        RegionManager.RequestNavigateAsync(Regions.SideMenuMain, itemDefinition.ViewType.Name);
    }

    public override async Task OnNavigatedToAsync(NavigationContext context)
    {
        await base.OnNavigatedToAsync(context);

        User? user = null;

        if (
            context.Parameters is { } parameters
            && parameters.TryGetValue<User>("Auth", out var navUser)
        )
        {
            user = navUser;
        }

        user ??= await _aniListService.GetAuthenticatedUserAsync();
        User = user;
    }

    [ScanForTypes(
        AssignableTo = typeof(UserControl<>),
        TypeNameFilter = "*PageView",
        Handler = nameof(GetSideMenuItemDefinitionHandler)
    )]
    private static partial SideMenuItemDefinition[] GetSideMenuItemDefinitions();

    private static SideMenuItemDefinition GetSideMenuItemDefinitionHandler<TView, TViewModel>()
        where TView : Control
        where TViewModel : PageViewModel => new(typeof(TView), typeof(TViewModel));

    private sealed record SideMenuItemDefinition(Type ViewType, Type ViewModelType);
}
