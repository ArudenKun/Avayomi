using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avayomi.Services;
using Avayomi.Services.Toasts;
using Avayomi.ViewModels.Components;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NinjaNye.SearchExtensions.Levenshtein;
using Volo.Abp.DependencyInjection;

namespace Avayomi.ViewModels.Pages;

[Dependency(ServiceLifetime.Singleton)]
public sealed partial class AnimePageViewModel : PageViewModel
{
    private readonly IAnimeService _animeService;

    public AnimePageViewModel(IAnimeService animeService)
    {
        _animeService = animeService;

        Animes = new AvaloniaList<AnimeCardViewModel>();
    }

    public override int Index => 1;

    public override LucideIconKind IconKind => LucideIconKind.Tv;

    public IAvaloniaList<AnimeCardViewModel> Animes { get; }

    [ObservableProperty]
    public partial string AnimeProvider { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [Required(AllowEmptyStrings = false)]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoResultsFound))]
    public override partial bool IsBusy { get; set; }

    public bool NoResultsFound => !IsBusy && Animes.Count is 0;

    public IAvaloniaList<string> AnimeProviders { get; } = new AvaloniaList<string>();

    public override void OnLoaded()
    {
        AnimeProviders.Clear();
        AnimeProviders.AddRange(_animeService.GetProviders());
        if (!GeneralSettings.Provider.IsNullOrEmpty())
        {
            _animeService.SetProvider(GeneralSettings.Provider);
        }

        AnimeProvider = _animeService.CurrentProvider;
    }

    public override void OnUnloaded()
    {
        base.OnUnloaded();
        AnimeProviders.Clear();
    }

    private bool CanSubmit() => !string.IsNullOrEmpty(Search) && !string.IsNullOrWhiteSpace(Search);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync(CancellationToken cancellationToken) =>
        await SetBusyAsync(
            async () =>
            {
                try
                {
                    Logger.LogInformation("{Provider} Searching: {Search}", AnimeProvider, Search);
                    Animes.Clear();
                    var animeResults = await _animeService.SearchAsync(Search, cancellationToken);
                    if (animeResults.Count is 0)
                    {
                        Logger.LogInformation("No results found");
                        return;
                    }

                    var animes = animeResults
                        .LevenshteinDistanceOf(x => x.Title, x => x.OtherNames)
                        .ComparedTo(Search)
                        .OrderBy(x => x.Distance)
                        .Select(x => x.Item);

                    Animes.AddRange(
                        animes.Select(x =>
                        {
                            var vm = ServiceProvider.GetRequiredService<AnimeCardViewModel>();
                            vm.Id = x.Id;
                            vm.Title = x.Title;
                            vm.CoverUrl = x.Image ?? string.Empty;
                            return vm;
                        })
                    );
                }
                catch (TaskCanceledException)
                {
                    var search = Search;
                    var providerName = AnimeProvider;
                    ToastService.ShowToast(
                        NotificationType.Warning,
                        "Search",
                        $"Search for {Search} has been canceled",
                        new ToastActionButton(
                            "Retry",
                            _ =>
                            {
                                Search = search;
                                AnimeProvider = providerName;
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

    partial void OnAnimeProviderChanged(string oldValue, string newValue)
    {
        if (!newValue.IsNullOrEmpty())
        {
            if (!GeneralSettings.Provider.Equals(newValue, StringComparison.OrdinalIgnoreCase))
            {
                GeneralSettings.Provider = newValue;
            }

            _animeService.SetProvider(newValue);
            if (CanSubmit())
            {
                SubmitCommand.Execute(null);
            }
        }

        Logger.LogInformation("Provider: {OldValue} => {NewValue}", oldValue, newValue);
    }
}
