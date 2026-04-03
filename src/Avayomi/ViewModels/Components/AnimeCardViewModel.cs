using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avayomi.ViewModels.Components;

public sealed partial class AnimeCardViewModel : ViewModel
{
    [ObservableProperty]
    public partial string Id { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Titles))]
    public partial string EnglishTitle { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Titles))]
    public partial string RomanjiTitle { get; set; } = string.Empty;

    public IReadOnlyList<string> Titles => [RomanjiTitle, EnglishTitle];
}
