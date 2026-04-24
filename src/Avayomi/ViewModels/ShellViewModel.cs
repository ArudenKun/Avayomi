using System;
using System.Threading.Tasks;
using AsyncNavigation;
using Avayomi.Extensions;
using Avayomi.Views;

namespace Avayomi.ViewModels;

public sealed class ShellViewModel : ViewModel
{
    public override Task OnNavigatedToAsync(NavigationContext context)
    {
        RegionManager.RequestNavigate<MainView>(
            Regions.Main,
            delay: TimeSpan.FromMilliseconds(100)
        );
        return Task.CompletedTask;
    }
}
