using Avayomi.ViewModels.Components;

namespace Avayomi.Views.Components;

public partial class AnimeCardView : UserControl<AnimeCardViewModel>, IViewNameProvider
{
    public AnimeCardView()
    {
        InitializeComponent();
    }

    public static string ViewName => nameof(AnimeCardView);
}
