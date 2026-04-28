using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace Avayomi.ViewModels;

[Dependency(ServiceLifetime.Singleton)]
public sealed class MainWindowViewModel : ViewModel
{
    public MainWindowViewModel(ShellViewModel shellViewModel)
    {
        ShellViewModel = shellViewModel;
    }

    public ShellViewModel ShellViewModel { get; }
}
