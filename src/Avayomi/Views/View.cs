using Avalonia.Interactivity;
using Avayomi.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Avayomi.Views;

public abstract class View<TViewModel> : ViewBase<TViewModel>, IView<TViewModel>
    where TViewModel : ViewModel
{
    protected View(TViewModel viewModel)
        : base(viewModel)
    {
        AddHandler(LoadedEvent, Handler);
        AddHandler(UnloadedEvent, Handler);
    }

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
