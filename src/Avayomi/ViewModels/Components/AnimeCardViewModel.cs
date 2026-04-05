using Avayomi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avayomi.ViewModels.Components;

public sealed partial class AnimeCardViewModel : ViewModel
{
    private readonly IAniListService _aniListService;

    public AnimeCardViewModel(IAniListService aniListService)
    {
        _aniListService = aniListService;
    }

    [ObservableProperty]
    public partial AnimeCardStatusButtonViewModel? StatusButton { get; set; }

    [ObservableProperty]
    public partial bool IsAuthenticated { get; set; }

    [ObservableProperty]
    public partial bool IsCoverLoading { get; set; }

    [ObservableProperty]
    public partial string Id { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CoverUrl { get; set; } = string.Empty;

    public override void OnLoaded()
    {
        IsAuthenticated = _aniListService.IsAuthenticated;

        SetupStatusButton();
    }

    private void SetupStatusButton()
    {
        var viewModel = ServiceProvider.GetRequiredService<AnimeCardStatusButtonViewModel>();
        StatusButton = viewModel;
    }
}
