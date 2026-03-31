using System.Text.Json;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Avayomi.AniList;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using Microsoft.Extensions.Logging;
using StrawberryShake;

namespace Avayomi.ViewModels.Pages;

public sealed partial class AnimePageViewModel : PageViewModel
{
    private readonly IAniListClient _aniListClient;

    public AnimePageViewModel(IAniListClient aniListClient)
    {
        _aniListClient = aniListClient;
    }

    public override int Index => 1;

    public override LucideIconKind IconKind => LucideIconKind.Tv;

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    public override void OnLoaded()
    {
        StartAsync().SafeFireAndForget();
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        var result = await _aniListClient.Search.ExecuteAsync(Search);
        result.EnsureNoErrors();

        if (result.Data is null)
        {
            Logger.LogInformation("No results found");
            return;
        }

        Logger.LogInformation("{Data}", JsonSerializer.Serialize(result.Data.Page?.Media));
    }

    private async Task StartAsync() { }
}
