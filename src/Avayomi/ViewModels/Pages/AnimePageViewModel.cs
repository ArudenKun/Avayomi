using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avayomi.Core.AniList;
using Avayomi.Core.Anime;
using Avayomi.Core.Providers.Anime;
using Avayomi.Services;
using Avayomi.Services.Toasts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Caching;

namespace Avayomi.ViewModels.Pages;

public sealed partial class AnimePageViewModel : PageViewModel
{
    private readonly IAniListClient _aniListClient;
    private readonly IDistributedCache<ICollection<AnimeInfo>> _animeInfoCache;
    private readonly ITokenService _tokenService;

    public AnimePageViewModel(
        IAniListClient aniListClient,
        IDistributedCache<ICollection<AnimeInfo>> animeInfoCache,
        ITokenService tokenService,
        IEnumerable<IAnimeProvider> animeProviders
    )
    {
        _aniListClient = aniListClient;
        _animeInfoCache = animeInfoCache;
        _tokenService = tokenService;
        AnimeProviders = animeProviders.ToDictionary(x => x.Name);

        Animes = new AvaloniaList<AnimeInfo>();
    }

    public override int Index => 1;

    public override LucideIconKind IconKind => LucideIconKind.Tv;

    public IAvaloniaList<AnimeInfo> Animes { get; }

    [ObservableProperty]
    public partial string ProviderName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [Required(AllowEmptyStrings = false)]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoResultsFound))]
    public override partial bool IsBusy { get; set; }

    public bool NoResultsFound => !IsBusy && Animes.Count is 0;

    public IReadOnlyDictionary<string, IAnimeProvider> AnimeProviders { get; }

    public override async void OnLoaded()
    {
        StartAsync().SafeFireAndForget();

        var cachedAccessToken = await _tokenService.GetAccessTokenAsync();
        Logger.LogInformation("AccessToken: {AccessToken}", cachedAccessToken);

        if (ProviderName.IsNullOrEmpty())
        {
            ProviderName = AnimeProviders.Keys.First();
        }
    }

    [RelayCommand]
    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        var result = await _tokenService.LoginAsync();
        if (!result)
        {
            DialogService.ShowErrorMessageBox("AniList Login", "Login Failed");
        }

        Logger.LogInformation("Login: {Token}", await _tokenService.GetAccessTokenAsync());
    }

    private bool CanSubmit() => !string.IsNullOrEmpty(Search) && !string.IsNullOrWhiteSpace(Search);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync(CancellationToken cancellationToken) =>
        await SetBusyAsync(
            async () =>
            {
                try
                {
                    var animeProvider = AnimeProviders[ProviderName];
                    Logger.LogInformation(
                        "{Provider} Searching: {Search}",
                        animeProvider.Name,
                        Search
                    );
                    Animes.Clear();
                    var result = await _animeInfoCache.GetOrAddAsync(
                        $"{Search.Trim()}-{ProviderName}",
                        async () =>
                            (await animeProvider.SearchAsync(Search, cancellationToken)).Map(),
                        token: cancellationToken
                    );
                    if (result.IsNullOrEmpty())
                    {
                        Logger.LogInformation("No results found");
                        return;
                    }

                    Animes.AddRange(result!);
                }
                catch (TaskCanceledException)
                {
                    var search = Search;
                    var providerName = ProviderName;
                    ToastService.ShowToast(
                        NotificationType.Warning,
                        "Search",
                        $"Search for {Search} has been canceled",
                        new ToastActionButton(
                            "Retry",
                            _ =>
                            {
                                Search = search;
                                ProviderName = providerName;
                                SubmitCommand.Execute(null);
                            },
                            true
                        )
                    );
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "An exception occured while searching");
                    ToastService.ShowToast(NotificationType.Error, "Search", ex.Message);
                }
            },
            "Searching",
            false
        );

    private async Task StartAsync() { }

    partial void OnProviderNameChanged(string oldValue, string newValue)
    {
        Logger.LogInformation("Provider: {OldValue} => {NewValue}", oldValue, newValue);
    }
}

[Mapper]
public static partial class AnimeInfoMapper
{
    public static partial AnimeInfo Map(this IAnimeInfo animeInfo);

    public static partial ICollection<AnimeInfo> Map(this IEnumerable<IAnimeInfo> animeInfo);
}
