using Avayomi.ViewModels;

namespace Avayomi.Views;

public interface IView<TViewModel>
    where TViewModel : ViewModel
{
    TViewModel ViewModel { get; }
    TViewModel DataContext { get; set; }
}
