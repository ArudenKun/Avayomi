using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avayomi.Services;
using Avayomi.ViewModels.Components;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PleasantUI;
using Raffinert.FuzzySharp;
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

    public override Geometry IconKind => MaterialIcons.Video;

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
                        .Select(anime =>
                        {
                            if (
                                anime
                                    .Title.ToLowerInvariant()
                                    .Equals(Search, StringComparison.OrdinalIgnoreCase)
                                || anime.AlternativeTitles.Any(alt =>
                                    alt.ToLowerInvariant()
                                        .Equals(Search, StringComparison.OrdinalIgnoreCase)
                                )
                            )
                            {
                                return new { Anime = anime, Score = 1000 };
                            }

                            var titleScore = Fuzz.WeightedRatio(
                                Search.ToLowerInvariant(),
                                anime.Title.ToLowerInvariant()
                            );
                            var altScore = anime
                                .AlternativeTitles.Select(alt =>
                                    Fuzz.WeightedRatio(
                                        Search.ToLowerInvariant(),
                                        alt.ToLowerInvariant()
                                    )
                                )
                                .DefaultIfEmpty(0)
                                .Max();

                            var score = Math.Max(titleScore, altScore);

                            if (anime.Title.StartsWith(Search, StringComparison.OrdinalIgnoreCase))
                            {
                                score += 50;
                            }

                            return new { Anime = anime, Score = score };
                        })
                        .OrderByDescending(x => x.Score)
                        .Select(x => x.Anime)
                        .ToList();

                    Logger.LogInformation("Animes Score: {Json}", JsonSerializer.Serialize(animes));

                    Animes.AddRange(
                        animes.Select(x =>
                        {
                            var vm = ServiceProvider.GetRequiredService<AnimeCardViewModel>();
                            vm.Id = x.Id;
                            vm.Title = x.Title;
                            vm.CoverUrl = x.Images.Medium;
                            return vm;
                        })
                    );
                }
                catch (TaskCanceledException)
                {
                    var search = Search;
                    var providerName = AnimeProvider;
                    // ToastService.ShowToast(
                    //     NotificationType.Warning,
                    //     "Search",
                    //     $"Search for {Search} has been canceled",
                    //     new ToastActionButton(
                    //         "Retry",
                    //         _ =>
                    //         {
                    //             Search = search;
                    //             AnimeProvider = providerName;
                    //             SubmitCommand.Execute(null);
                    //         },
                    //         true
                    //     )
                    // );
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "An exception occured while searching");
                    // ToastService.ShowToast(NotificationType.Error, "Search", ex.Message);
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
