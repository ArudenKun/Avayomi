using Avayomi.ViewModels;

namespace Avayomi.Views;

public partial class MainView : UserControl<MainViewModel>, IViewNameProvider
{
    public MainView()
    {
        InitializeComponent();
    }

    public static string ViewName => nameof(MainView);
}
