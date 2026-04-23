using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avayomi.ViewModels;
using Flowery.Controls;

namespace Avayomi.Views;

public sealed class SplashView : View<SplashViewModel>
{
    protected override object Build(SplashViewModel vm) =>
        new Panel().Children(
            new StackPanel()
                .VerticalAlignment(VerticalAlignment.Center)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Spacing(4)
                .Children(
                    new Image()
                        .Width(120)
                        .Height(120)
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Source(AvaloniaResources.logo_png.AsBitmap())
                ),
            new StackPanel()
                .Margin(40)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Bottom)
                .Spacing(6)
                .Children(
                    new DaisyProgress().Height(6).IsIndeterminate(true),
                    new TextBlock()
                        .Margin(0, 4, 0, 0)
                        .FontSize(20)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .Text(vm, x => x.BusyText, BindingMode.OneWay)
                )
        );
}
