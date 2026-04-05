using Avalonia.Input;
using Avayomi.ViewModels.Pages;

namespace Avayomi.Views.Pages;

public partial class AnimePageView : UserControl<AnimePageViewModel>
{
    public AnimePageView()
    {
        InitializeComponent();

        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter)
            return;

        if (ViewModel.SubmitCommand.CanExecute(null))
        {
            ViewModel.SubmitCommand.Execute(null);
        }
    }
}
