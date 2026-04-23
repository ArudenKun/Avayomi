using Avalonia.Markup.Declarative;
using Avayomi.ViewModels;

namespace Avayomi.Views;

public abstract class View<TViewModel> : ViewBase<TViewModel>, IView<TViewModel>
    where TViewModel : ViewModel;
