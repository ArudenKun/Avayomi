using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avayomi.ViewModels.Dialogs;

namespace Avayomi.Views.Dialogs;

public partial class SettingsDialogView : UserControl<SettingsDialogViewModel>
{
    public SettingsDialogView()
    {
        InitializeComponent();
    }
}
