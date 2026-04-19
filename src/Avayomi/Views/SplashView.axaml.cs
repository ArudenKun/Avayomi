using Avayomi.ViewModels;

namespace Avayomi.Views;

public partial class SplashView : UserControl<SplashViewModel>, IViewNameProvider
{
    public SplashView()
    {
        InitializeComponent();
    }

    public static string ViewName => nameof(SplashView);
}
