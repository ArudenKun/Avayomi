using Avayomi.ViewModels.Pages;

namespace Avayomi.Views.Pages;

public sealed class AboutPageView : View<AboutPageViewModel>
{
    protected override object Build(AboutPageViewModel vm) =>
        new TextBlock().Text(nameof(AboutPageView));
}
