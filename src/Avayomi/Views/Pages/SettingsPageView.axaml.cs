using Avayomi.ViewModels.Pages;

namespace Avayomi.Views.Pages;

public partial class SettingsPageView : UserControl<SettingsPageViewModel>, IViewNameProvider
{
    public SettingsPageView()
    {
        InitializeComponent();
    }

    public static string ViewName => nameof(SettingsPageView);
}
