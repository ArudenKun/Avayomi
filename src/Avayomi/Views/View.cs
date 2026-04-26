using Avalonia.Interactivity;
using Avayomi.ViewModels;

namespace Avayomi.Views;

public abstract class View<TViewModel> : ViewBase<TViewModel>, IView<TViewModel>
    where TViewModel : ViewModel
{
    protected View()
    {
        AddHandler(LoadedEvent, Handler);
        AddHandler(UnloadedEvent, Handler);
    }

    private void Handler(object? sender, RoutedEventArgs e)
    {
        if (e.RoutedEvent == LoadedEvent)
        {
            if (ViewModel is { } viewModel)
            {
                viewModel.OnLoaded();
            }
        }

        if (e.RoutedEvent == UnloadedEvent)
        {
            if (ViewModel is { } viewModel)
            {
                viewModel.OnUnloaded();
            }
        }
    }
}
