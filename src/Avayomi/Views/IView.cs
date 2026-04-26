using AsyncNavigation.Abstractions;
using Avayomi.ViewModels;

namespace Avayomi.Views;

public interface IView<out TViewModel> : IView
    where TViewModel : ViewModel
{
    TViewModel? ViewModel { get; }
}
