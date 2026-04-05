using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avayomi.Core.AniList.Models.User;
using Avayomi.Messaging.Messages;
using Avayomi.Services;
using Avayomi.ViewModels.Pages;
using Avayomi.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Lucide.Avalonia;
using Microsoft.Extensions.Logging;
using ServiceScan.SourceGenerator;
using SukiUI.Controls;
using ZLinq;

namespace Avayomi.ViewModels;

public sealed partial class MainViewModel : ViewModel, IRecipient<ChangePageMessage>
{
    private readonly IAniListService _aniListService;

    public MainViewModel(IEnumerable<PageViewModel> pageViewModels, IAniListService aniListService)
    {
        _aniListService = aniListService;

        var orderedPageViewModels = pageViewModels
            .AsValueEnumerable()
            .OrderBy(x => x.Index)
            .ToList();

        Pages.AddRange(
            orderedPageViewModels.Select(x => new SukiSideMenuItem
            {
                Header = x.DisplayName,
                IsVisible = x.IsVisibleOnSideMenu,
                Icon = new LucideIcon
                {
                    Width = 20,
                    Height = 20,
                    Kind = x.IconKind,
                },
                Tag = x.GetType(),
            })
        );
    }

    [ObservableProperty]
    public partial SukiSideMenuItem? Page { get; set; }

    public IAvaloniaList<SukiSideMenuItem> Pages { get; } = new AvaloniaList<SukiSideMenuItem>();

    [ObservableProperty]
    public partial User? User { get; set; }

    public override void OnLoaded()
    {
        base.OnLoaded();

        // Assigning PageItem instead of calling ChangePage directly.
        // This keeps the UI selection visually in sync with the current page.
        Page = Pages.FirstOrDefault(x => x.IsVisible);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _aniListService.LogoutAsync();
        await NavigationHostManager.NavigateAsync<LoginView>(HostNames.Main);
    }

    public void Receive(ChangePageMessage message)
    {
        var item = Pages.FirstOrDefault(x => x.Tag as Type == message.ViewModelType);
        if (item is null)
        {
            ToastService.ShowToast(
                NotificationType.Error,
                "Navigation Error",
                "An internal error occurred while navigating to the page."
            );
            return;
        }

        // Setting PageItem automatically triggers OnPageItemChanged -> ChangePage
        Page = item;
    }

    partial void OnPageChanged(SukiSideMenuItem? value)
    {
        if (value?.Tag is not Type viewModelType)
            return;
        Logger.LogInformation("PageItemChange {Header}", value.Header);
        ChangeContent(viewModelType);
    }

    private void ChangeContent(Type viewModelType)
    {
        var itemDefinition = GetSideMenuItemDefinitions()
            .FirstOrDefault(x => x.ViewModelType == viewModelType);
        if (itemDefinition is null)
            return;
        NavigationHostManager.Navigate(HostNames.SideMenuMain, itemDefinition.ViewType);
    }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        if (parameter is not User user)
        {
            user = await _aniListService.GetAuthenticatedUserAsync();
        }

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
