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
        if (e.Key == Key.Enter)
        {
            if (ViewModel.SubmitCommand.CanExecute(null))
            {
                ViewModel.SubmitCommand.Execute(null);
            }
        }
    }
}
