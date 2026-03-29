using Avalonia;
using Avalonia.Controls;
using Lucide.Avalonia;

namespace Avayomi.Controls;

public partial class SettingsRow : UserControl
{
    public SettingsRow()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<LucideIconKind> IconKindProperty =
        AvaloniaProperty.Register<SettingsRow, LucideIconKind>(nameof(IconKind));

    public LucideIconKind IconKind
    {
        get => GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public static readonly StyledProperty<string?> HeaderProperty = AvaloniaProperty.Register<
        SettingsRow,
        string?
    >(nameof(Header));

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<
        SettingsRow,
        string?
    >(nameof(Description));

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}
