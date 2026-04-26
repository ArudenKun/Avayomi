using System.Collections.Generic;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avayomi.Converters;
using Avayomi.Extensions;
using Avayomi.ViewModels.Pages;
using PleasantUI;
using PleasantUI.Controls;
using PleasantUI.Core.Models;
using PleasantUI.ToolKit.UserControls;

namespace Avayomi.Views.Pages;

public sealed class SettingsPageView : View<SettingsPageViewModel>
{
    private static readonly IValueConverter IsNotNullOrEmptyConverter = new FuncValueConverter<
        ICollection<object>,
        bool
    >(values => !values.IsNullOrEmpty());

    private SmoothScrollViewer _parent = null!;

    protected override object Build(SettingsPageViewModel vm) =>
        new SmoothScrollViewer()
            .Ref(out _parent)
            .Content(
                new StackPanel()
                    .Margin(25)
                    .Spacing(5)
                    .Children(
                        new TextBlock()
                            .Text("Settings")
                            .Margin(0, 0, 0, 10)
                            .DynamicResource(ThemeProperty, "TitleTextBlockTheme"),
                        new OptionsDisplayItem()
                            .Header("Log Level")
                            .ActionButton(
                                new ComboBox()
                                    .MinWidth(150)
                                    .ItemsSource(
                                        vm,
                                        x => x.LoggingSettings.LogEventLevel,
                                        BindingMode.OneTime,
                                        EnumToEnumerableConverter.Instance
                                    )
                                    .SelectedItem(
                                        vm,
                                        x => x.LoggingSettings.LogEventLevel,
                                        BindingMode.TwoWay
                                    )
                                    .ItemTemplate<string>(x => new TextBlock().Text(x))
                            ),
                        new OptionsDisplayItem()
                            .Header("Theme")
                            .Icon(MaterialIcons.BrushVariant)
                            .Expands(true)
                            .ActionButton(
                                new ComboBox()
                                    .ItemsSource(PleasantTheme.Themes)
                                    .SelectedItem(vm, x => x.Theme, BindingMode.TwoWay)
                                    .ItemTemplate<Theme>(theme =>
                                        new StackPanel()
                                            .Spacing(10)
                                            .Orientation(Orientation.Horizontal)
                                            .Children(
                                                new ThemePreviewVariantScope()
                                                    .IsVisible(
                                                        theme,
                                                        x => x.ThemeVariant,
                                                        BindingMode.TwoWay,
                                                        ObjectConverters.IsNotNull
                                                    )
                                                    .RequestedThemeVariant(
                                                        theme,
                                                        x => x.ThemeVariant,
                                                        BindingMode.TwoWay
                                                    )
                                                    .Child(new ThemePreview()),
                                                new TextBlock().Text(theme, x => x.Name)
                                            )
                                    )
                            )
                            .Content(
                                new StackPanel()
                                    .Spacing(5)
                                    .Children(
                                        new Panel().Children(
                                            new TextBlock()
                                                .Text("Custom Themes")
                                                .DynamicResource(
                                                    ThemeProperty,
                                                    "BaseTextBlockTheme"
                                                )
                                                .VerticalAlignment(VerticalAlignment.Center)
                                        ),
                                        new Button()
                                            .HorizontalAlignment(HorizontalAlignment.Right)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                            .Width(40)
                                            .Height(40)
                                            .Command(
                                                vm,
                                                x => x.CreateThemeCommand,
                                                BindingMode.TwoWay
                                            )
                                            .Content(new PathIcon().Data(MaterialIcons.Plus)),
                                        new Border()
                                            .DynamicResource(
                                                Border.CornerRadiusProperty,
                                                "ControlCornerRadius"
                                            )
                                            .DynamicResource(
                                                Border.BackgroundProperty,
                                                "BackgroundColor2"
                                            )
                                            .Height(250)
                                            .Child(
                                                new Panel().Children(
                                                    new ListBox()
                                                        .ItemsSource(PleasantTheme.CustomThemes)
                                                        .SelectedItem(
                                                            vm,
                                                            x => x.CustomTheme,
                                                            BindingMode.TwoWay
                                                        )
                                                        .Padding(5, 5, 5, 5)
                                                        .ItemTemplate<CustomTheme>(customTheme =>
                                                            new Grid()
                                                                .Cols("Auto,*,Auto")
                                                                .Children(
                                                                    new ThemePreviewVariantScope()
                                                                        .Grid_Column(0)
                                                                        .RequestedThemeVariant(
                                                                            customTheme,
                                                                            x => x.ThemeVariant,
                                                                            BindingMode.TwoWay
                                                                        ),
                                                                    new TextBlock()
                                                                        .Grid_Column(1)
                                                                        .Margin(10, 0, 0, 0)
                                                                        .Text(
                                                                            customTheme,
                                                                            x => x.Name,
                                                                            BindingMode.TwoWay
                                                                        )
                                                                        .VerticalAlignment(
                                                                            VerticalAlignment.Center
                                                                        )
                                                                        .TextTrimming(
                                                                            TextTrimming.CharacterEllipsis
                                                                        ),
                                                                    new StackPanel()
                                                                        .Grid_Column(2)
                                                                        .IsVisible(
                                                                            _parent,
                                                                            x => x.IsPointerOver,
                                                                            BindingMode.TwoWay
                                                                        )
                                                                        .Orientation(
                                                                            Orientation.Horizontal
                                                                        )
                                                                        .Spacing(5)
                                                                        .Children(
                                                                            new Button()
                                                                                .DynamicResource(
                                                                                    ThemeProperty,
                                                                                    "AppBarButtonTheme"
                                                                                )
                                                                                .Width(30)
                                                                                .Height(30)
                                                                                .Command(
                                                                                    vm,
                                                                                    x =>
                                                                                        x.EditThemeCommand,
                                                                                    BindingMode.TwoWay
                                                                                )
                                                                                .CommandParameter(
                                                                                    customTheme
                                                                                )
                                                                                .Content(
                                                                                    new PathIcon().Data(
                                                                                        MaterialIcons.PencilOutline
                                                                                    )
                                                                                ),
                                                                            new Button()
                                                                                .DynamicResource(
                                                                                    ThemeProperty,
                                                                                    "DangerButtonTheme"
                                                                                )
                                                                                .DynamicResource(
                                                                                    TemplatedControl.CornerRadiusProperty,
                                                                                    "ControlCornerRadius"
                                                                                )
                                                                                .BorderThickness(0)
                                                                                .Width(30)
                                                                                .Height(30)
                                                                                .Command(
                                                                                    vm,
                                                                                    x =>
                                                                                        x.DeleteThemeCommand,
                                                                                    BindingMode.TwoWay
                                                                                )
                                                                                .CommandParameter(
                                                                                    customTheme
                                                                                )
                                                                                .Content(
                                                                                    new PathIcon().Data(
                                                                                        MaterialIcons.DeleteOutline
                                                                                    )
                                                                                )
                                                                        )
                                                                )
                                                        ),
                                                    new TextBlock()
                                                        .Text("No Custom Themes")
                                                        .IsVisible(
                                                            PleasantTheme.CustomThemes,
                                                            x => x.Count,
                                                            BindingMode.TwoWay,
                                                            IsNotNullOrEmptyConverter
                                                        )
                                                        .VerticalAlignment(VerticalAlignment.Center)
                                                        .TextAlignment(TextAlignment.Center)
                                                        .DynamicResource(
                                                            TextBlock.ForegroundProperty,
                                                            "TextFillColor3"
                                                        )
                                                )
                                            )
                                    )
                            )
                    )
            );
}
