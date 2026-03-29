using Avalonia.Collections;
using CommunityToolkit.Mvvm.Input;
using ZLinq;

namespace Avayomi.ViewModels.Dialogs;

public sealed partial class SettingsDialogViewModel : DialogViewModel
{
    public override string DialogTitle => "Settings";

    public IAvaloniaReadOnlyList<string> ColorThemes =>
        new AvaloniaList<string>(
            ThemeService.ColorThemes.AsValueEnumerable().Select(x => x.DisplayName).ToList()
        );
}
