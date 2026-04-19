using Avayomi.ViewModels;

namespace Avayomi.Views;

public partial class MainWindow : SukiWindow<MainWindowViewModel>, IViewNameProvider
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public static string ViewName => nameof(MainWindow);
}
