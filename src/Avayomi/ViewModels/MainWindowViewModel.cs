using Avayomi.Core.Attributes;
using Avayomi.ViewModels.Abstractions;

namespace Avayomi.ViewModels;

[Singleton]
public sealed class MainWindowViewModel : BaseViewModel
{
    public MainWindowViewModel(ShellViewModel shellViewModel)
    {
        ViewContent = shellViewModel;
    }

    public object ViewContent { get; }
}