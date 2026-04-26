using Avalonia;
using Avalonia.Animation;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avayomi.Extensions;
using Humanizer;
using PleasantUI.Controls;

namespace Avayomi.Views.Components;

public sealed class BusyArea : ViewBase
{
    private static readonly IValueConverter BusyToOverlayConverter = new FuncValueConverter<
        bool,
        double
    >(value => value ? 1d : 0d);
    private static readonly IValueConverter BusyToContentOpacityConverter = new FuncValueConverter<
        bool,
        double
    >(value => value ? 0.35d : 1d);

    public static readonly StyledProperty<bool> IsBusyProperty = AvaloniaProperty.Register<
        BusyArea,
        bool
    >(nameof(IsBusy));

    public bool IsBusy
    {
        get => GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public static readonly StyledProperty<string> BusyTextProperty = AvaloniaProperty.Register<
        BusyArea,
        string
    >(nameof(BusyText), string.Empty);

    public string BusyText
    {
        get => GetValue(BusyTextProperty);
        set => SetValue(BusyTextProperty, value);
    }

    public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<
        BusyArea,
        object?
    >(nameof(Content));

    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    protected override object Build() =>
        new Panel().Children(
            new ContentControl()
                .Transitions([
                    new DoubleTransition().Duration(300.Milliseconds()).Property(OpacityProperty),
                ])
                .Opacity(this, x => x.IsBusy, BindingMode.OneWay, BusyToContentOpacityConverter)
                .IsHitTestVisible(this, x => !x.IsBusy, BindingMode.OneWay)
                .Content(this, x => x.Content),
            new Border()
                .Transitions([
                    new DoubleTransition().Duration(300.Milliseconds()).Property(OpacityProperty),
                ])
                .Opacity(this, x => x.IsBusy, BindingMode.OneWay, BusyToOverlayConverter)
                .IsHitTestVisible(this, x => x.IsBusy, BindingMode.OneWay)
                .Child(
                    new Panel().Children(
                        new Border()
                            .Opacity(0.72)
                            .DynamicResource(Border.BackgroundProperty, "BackgroundColor1"),
                        new StackPanel()
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Children(
                                new ProgressRing()
                                    .IsIndeterminate(true)
                                    .Width(96)
                                    .Height(96)
                                    .Thickness(6)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center),
                                new TextBlock()
                                    .Margin(0, 10, 0, 0)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .FontWeight(FontWeight.DemiBold)
                                    .FontSize(36)
                                    .IsVisible(
                                        this,
                                        x => x.BusyText,
                                        BindingMode.OneWay,
                                        StringConverters.IsNotNullOrEmpty
                                    )
                                    .Text(this, x => x.BusyText, BindingMode.OneWay)
                            )
                    )
                )
        );
}
