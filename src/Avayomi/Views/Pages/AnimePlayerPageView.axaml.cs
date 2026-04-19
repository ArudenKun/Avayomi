using Avayomi.ViewModels.Pages;

namespace Avayomi.Views.Pages;

public partial class AnimePlayerPageView : UserControl<AnimePlayerPageViewModel>, IViewNameProvider
{
    public AnimePlayerPageView()
    {
        InitializeComponent();
    }

    public static string ViewName => nameof(AnimePlayerPageView);
}
