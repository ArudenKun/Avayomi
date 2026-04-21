using System.Threading.Tasks;
using AsyncNavigation;
using Avayomi.Extensions;
using Avayomi.Views;

namespace Avayomi.ViewModels;

public sealed class ShellViewModel : ViewModel
{
    public override Task OnNavigatedToAsync(NavigationContext context)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000);
            await RegionManager.RequestNavigateAsync<MainView>(Regions.Main);
        });
        return Task.CompletedTask;
    }
}
