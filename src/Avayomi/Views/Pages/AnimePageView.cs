using Avayomi.ViewModels.Pages;

namespace Avayomi.Views.Pages;

public sealed class AnimePageView : View<AnimePageViewModel>
{
    protected override object Build(AnimePageViewModel vm) =>
        new TextBlock().Text(nameof(AnimePageView));
}
