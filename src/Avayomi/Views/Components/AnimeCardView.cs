using Avayomi.Extensions;
using Avayomi.ViewModels.Components;

namespace Avayomi.Views.Components;

public sealed class AnimeCardView : View<AnimeCardViewModel>
{
    public AnimeCardView(AnimeCardViewModel viewModel)
        : base(viewModel)
    {
    }

    protected override object Build(AnimeCardViewModel vm) => new BusyArea().;
}
