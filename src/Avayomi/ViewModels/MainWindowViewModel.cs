using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avayomi.ViewModels.Abstractions;
using CommunityToolkit.Mvvm.Input;

namespace Avayomi.ViewModels;

public sealed partial class MainWindowViewModel : BaseViewModel
{
    public string Greeting => "Welcome to Avalonia!";

    [RelayCommand]
    private void ShuwtDown()
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime applicationLifetime)
        {
            applicationLifetime.Shutdown();
        }
    }
}