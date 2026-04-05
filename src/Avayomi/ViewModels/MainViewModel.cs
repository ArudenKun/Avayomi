using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Avalonia.Collections;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SukiUI.Controls;
using ZLinq;

namespace Avayomi.ViewModels;

public sealed partial class MainViewModel : ViewModel, IRecipient<ChangePageMessage>
{
    private readonly IAniListService _aniListService;
    private readonly HashSet<Type> _pageTypes;

    public MainViewModel(IServiceProvider serviceProvider, IAniListService aniListService)
    {
        _aniListService = aniListService;

        // 1. Create the initial structure.
        var orderedPages = serviceProvider
            .GetRequiredService<IEnumerable<PageViewModel>>()
            .AsValueEnumerable()
            .OrderBy(x => x.Index)
            .Cast<PageViewModel>()
            .ToList();

        Pages.AddRange(
            orderedPages.Select(x => new SukiSideMenuItem
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

        // 2. Cache valid types for O(1) lookups during navigation
        _pageTypes = orderedPages.Select(vm => vm.GetType()).ToHashSet();
    }

    // Marked as nullable to prevent null-reference exceptions on startup before first load
    [ObservableProperty]
    public partial PageViewModel? Page { get; set; }

    [ObservableProperty]
    public partial SukiSideMenuItem? PageItem { get; set; }

    public IAvaloniaList<SukiSideMenuItem> Pages { get; } = new AvaloniaList<SukiSideMenuItem>();

    [ObservableProperty]
    public partial User? User { get; set; }

    public override void OnLoaded()
    {
        base.OnLoaded();

        // Assigning PageItem instead of calling ChangePage directly.
        // This keeps the UI selection visually in sync with the current page.
        PageItem = Pages.FirstOrDefault(x => x.IsVisible);

        LoadAsync().SafeFireAndForget();
    }

    private async Task LoadAsync()
    {
        User = await _aniListService.GetAuthenticatedUserAsync();
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
        PageItem = item;
    }

    partial void OnPageItemChanged(SukiSideMenuItem? value)
    {
        if (value?.Tag is not Type viewModelType)
            return;
        Logger.LogInformation("PageItemChange {Header}", value.Header);
        ChangePage(viewModelType);
    }

    private void ChangePage(Type viewModelType)
    {
        if (!_pageTypes.Contains(viewModelType))
            return;

        var newPage = (PageViewModel)ServiceProvider.GetRequiredService(viewModelType);

        // Handle Cleanup (Crucial for Transient ViewModels)
        var oldPage = Page;
        if (!ReferenceEquals(oldPage, newPage) && oldPage is IDisposable disposableVm)
        {
            try
            {
                disposableVm.Dispose();
            }
            catch (Exception e)
            {
                Logger.LogError(
                    e,
                    "An exception occurred while disposing the old page: {PageType}",
                    oldPage.GetType().Name
                );
            }
        }

        Page = newPage;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            try
            {
                Page?.Dispose();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to dispose the current page during teardown.");
            }
        }
    }
}
