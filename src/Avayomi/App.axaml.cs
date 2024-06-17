using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avayomi.ViewModels;

#if ENABLE_XAML_HOT_RELOAD
using HotAvalonia;
#endif

namespace Avayomi;

public class App : Application
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public App(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
    }

    public override void Initialize()
    {
#if ENABLE_XAML_HOT_RELOAD
        this.EnableHotReload();
#endif
        AvaloniaXamlLoader.Load(this);
    }

    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "<Pending>")]
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = DataTemplates[0].Build(_mainWindowViewModel) as Window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}