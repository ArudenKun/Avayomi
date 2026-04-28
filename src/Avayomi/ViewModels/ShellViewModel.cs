using System.Threading.Tasks;
using AsyncNavigation;
using Avayomi.Extensions;
using Avayomi.Views;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Avayomi.ViewModels;

[Dependency(ServiceLifetime.Singleton)]
public sealed class ShellViewModel : ViewModel
{
    public override void OnLoaded()
    {
        RegionManager.RequestNavigate<LoginView>(
            Regions.Main,
            onComplete: result =>
            {
                Logger.LogWarning(
                    "Result {0}: {1}",
                    result.Status.ToString(),
                    result.Exception?.Message
                );
            }
        );
    }
}
