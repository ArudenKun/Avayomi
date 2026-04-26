using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using PleasantUI.Controls;

namespace Avayomi.Views;

public sealed class SplashScreen : IPleasantSplashScreen
{
    private readonly SplashView _splashView;

    public SplashScreen()
    {
        SplashScreenContent = _splashView = ViewFactory.Create<SplashView>();
    }

    public string? AppName => null;
    public IImage? AppIcon => null;
    public object SplashScreenContent { get; }
    public IBrush? Background => null;
    public int MinimumShowTime => 2;

    public async Task RunTasks(CancellationToken cancellationToken)
    {
        _splashView.StatusText = "Loading Settings";
        await Task.Delay(1000, cancellationToken);
        _splashView.StatusText = "Finished";
        await Task.Delay(1000, cancellationToken);
    }
}
