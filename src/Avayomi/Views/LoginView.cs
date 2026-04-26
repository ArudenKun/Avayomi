using Avayomi.ViewModels;

namespace Avayomi.Views;

public sealed class LoginView : View<LoginViewModel>
{
    protected override object Build(LoginViewModel vm) => new Panel();
}
