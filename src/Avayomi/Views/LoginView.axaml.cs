using Avayomi.ViewModels;

namespace Avayomi.Views;

public partial class LoginView : UserControl<LoginViewModel>, IViewNameProvider
{
    public LoginView()
    {
        InitializeComponent();
    }

    public static string ViewName => nameof(LoginView);
}
