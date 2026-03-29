using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avayomi.ViewModels;

namespace Avayomi.Views;

public partial class MainView : UserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }
}
