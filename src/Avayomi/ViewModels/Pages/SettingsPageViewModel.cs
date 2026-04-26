using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avayomi.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PleasantUI;
using PleasantUI.Core;
using PleasantUI.Core.Models;
using PleasantUI.ToolKit;

namespace Avayomi.ViewModels.Pages;

public sealed partial class SettingsPageViewModel : PageViewModel
{
    public SettingsPageViewModel()
    {
        // IsVisibleOnSideMenu = false;
    }

    public required PleasantTheme PleasantTheme { private get; init; }

    public Theme? Theme
    {
        get =>
            PleasantSettings.Current?.Theme is { } themeName
                ? PleasantTheme.Themes.FirstOrDefault(theme => theme.Name == themeName)
                : null;
        set
        {
            if (PleasantSettings.Current is not null)
            {
                PleasantSettings.Current.Theme = value?.Name ?? "System";
                Debug.WriteLine(
                    $"[SettingsPageViewModel] Theme changed to {PleasantSettings.Current.Theme}"
                );
            }
        }
    }

    public CustomTheme? CustomTheme
    {
        get => PleasantTheme.SelectedCustomTheme;
        set => PleasantTheme.SelectedCustomTheme = value;
    }

    [RelayCommand]
    private async Task CreateThemeAsync()
    {
        var newCustomTheme = await ThemeEditorWindow.EditTheme(
            CachedServiceProvider.GetRequiredService<MainWindow>(),
            null
        );

        if (newCustomTheme is null)
            return;

        PleasantTheme.CustomThemes.Add(newCustomTheme);
    }

    [RelayCommand]
    private async Task EditThemeAsync(CustomTheme customTheme)
    {
        CustomTheme? newCustomTheme = await ThemeEditorWindow.EditTheme(
            CachedServiceProvider.GetRequiredService<MainWindow>(),
            customTheme
        );

        if (newCustomTheme is null)
            return;

        PleasantTheme.EditCustomTheme(customTheme, newCustomTheme);
    }

    [RelayCommand]
    private void DeleteTheme(CustomTheme customTheme)
    {
        PleasantTheme.CustomThemes.Remove(customTheme);
    }
}
