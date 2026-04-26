using Avalonia;
using Avalonia.Data;
using Avalonia.Layout;

namespace Avayomi.Views;

public sealed class SplashView : ViewBase
{
    public static readonly StyledProperty<string> StatusTextProperty = AvaloniaProperty.Register<
        SplashView,
        string
    >(nameof(StatusText));

    public string StatusText
    {
        get => GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    protected override object Build() =>
        new Panel().Children(
            new Image()
                .Width(120)
                .Height(120)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .VerticalAlignment(VerticalAlignment.Center)
                .Source(AvaloniaResources.logo_png.AsBitmap()),
            new StackPanel()
                .Margin(40)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Bottom)
                .Spacing(6)
                .Children(
                    new ProgressBar().Height(6).IsIndeterminate(true),
                    new TextBlock()
                        .Margin(0, 4, 0, 0)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .FontSize(20)
                        .Text(this, x => x.StatusText, BindingMode.TwoWay)
                )
        );
}
