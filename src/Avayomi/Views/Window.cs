using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avayomi.Utilities;
using Avayomi.ViewModels;
using Volo.Abp.DependencyInjection;

namespace Avayomi.Views;

public abstract class Window<TViewModel> : Window, IView<TViewModel>, ITransientDependency
    where TViewModel : ViewModel
{
    public new TViewModel DataContext
    {
        get =>
            base.DataContext as TViewModel
            ?? throw new InvalidCastException(
                $"DataContext is null or not of the expected type '{typeof(TViewModel).FullName}'."
            );
        set => base.DataContext = value;
    }

    public TViewModel ViewModel => DataContext;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        DispatchHelper.Invoke(() => ViewModel.OnLoaded());
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        DispatchHelper.Invoke(() => ViewModel.OnUnloaded());
    }
}
