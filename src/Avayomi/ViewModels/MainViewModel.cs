using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncNavigation.Core;
using Avalonia.Collections;
using Avalonia.Controls;
using Avayomi.Core.AniList.Models.User;
using Avayomi.Extensions;
using Avayomi.Messaging.Messages;
using Avayomi.Services;
using Avayomi.ViewModels.Pages;
using Avayomi.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using PleasantUI.Controls;
using ZLinq;

namespace Avayomi.ViewModels;

public sealed partial class MainViewModel : NavigationViewModel, IRecipient<ChangePageMessage>
{
    private readonly IAniListService _aniListService;

    private readonly Stack<NavigationViewItem> _backStack = new();

    public MainViewModel(
        IEnumerable<PageViewModel> pageViewModels,
        IAniListService aniListService,
        IServiceProvider serviceProvider
    )
    {
        _aniListService = aniListService;

        var pages = pageViewModels.AsValueEnumerable().OrderBy(x => x.Index);
        Pages.AddRange([
            .. pages.Select(x =>
            {
                var navigationViewItem = new NavigationViewItem
                {
                    Header = x.DisplayName,
                    Icon = x.IconKind,
                    Content = serviceProvider.GetRequiredService<ViewLocator>().CreateView(x),
                    Tag = new Tag(x.GetType(), x),
                };
                if (!x.IsTopLevel)
                {
                    DockPanel.SetDock(navigationViewItem, Dock.Bottom);
                }
                return navigationViewItem;
            }),
        ]);
        Page = Pages.FirstOrDefault();
    }

    [ObservableProperty]
    public partial NavigationViewItem? Page { get; set; }

    public IAvaloniaList<NavigationViewItem> Pages { get; } =
        new AvaloniaList<NavigationViewItem>();

    partial void OnPageChanged(NavigationViewItem? oldValue, NavigationViewItem? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.Content = CreateContent(oldValue.Tag!.As<Tag>().ViewModel);
            _backStack.Push(oldValue);
        }
    }

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

    [RelayCommand]
    private async Task BackAsync()
    {
        Page = _backStack.Pop();
    }

    public void Receive(ChangePageMessage message)
    {
        var navigationViewItem = Pages.FirstOrDefault(x =>
            x.Tag!.As<Tag>().ViewModelType == message.ViewModelType
        );
        if (navigationViewItem is null)
        {
            // ToastService.ShowToast(
            //     NotificationType.Error,
            //     "Navigation Error",
            //     "An internal error occurred while navigating to the page."
            // );
            return;
        }

        navigationViewItem.Content = CreateContent(navigationViewItem.Tag!.As<Tag>().ViewModel);
        Page = navigationViewItem;
    }

    private Control CreateContent(PageViewModel viewModel) =>
        ServiceProvider.GetRequiredService<ViewLocator>().CreateView(viewModel);

    private record Tag(Type ViewModelType, PageViewModel ViewModel);
}
