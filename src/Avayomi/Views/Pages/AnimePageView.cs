using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avayomi.ViewModels.Components;
using Avayomi.ViewModels.Pages;
using Avayomi.Views.Components;
using PleasantUI.Controls;

namespace Avayomi.Views.Pages;

public sealed class AnimePageView : View<AnimePageViewModel>
{
    protected override object Build(AnimePageViewModel vm) =>
        new Grid()
            .Rows("Auto, *")
            .Children(
                new StackPanel()
                    .Grid_Row(0)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Orientation(Orientation.Horizontal)
                    .Children(
                        new TextBox().MinWidth(100).Text(vm, x => x.Search, BindingMode.TwoWay),
                        new Button()
                            .Command(vm, x => x.SubmitCommand)
                            .Content("Submit")
                            .IsEnabled(vm, x => !x.IsBusy),
                        new Button()
                            .Command(vm, x => x.SubmitCancelCommand)
                            .Content("Cancel")
                            .IsEnabled(vm, x => x.IsBusy),
                        new ComboBox()
                            .ItemsSource(vm, x => x.AnimeProviders)
                            .PlaceholderText("Providers")
                            .SelectedItem(vm, x => x.AnimeProvider)
                    ),
                new BusyArea()
                    .Grid_Row(1)
                    .BusyText(vm, x => x.BusyText)
                    .IsBusy(vm, x => x.IsBusy)
                    .Content(
                        new SmoothScrollViewer().Content(
                            new ItemsRepeater()
                                .ItemsSource(vm, x => x.Animes)
                                .ItemTemplate(
                                    new FuncDataTemplate<AnimeCardViewModel>(
                                        (viewModel, _) => new AnimeCardView(viewModel)
                                    )
                                )
                                .Layout(
                                    new UniformGridLayout()
                                        .ItemsStretch(UniformGridLayoutItemsStretch.Fill)
                                        .MinColumnSpacing(15)
                                        .MinRowSpacing(5)
                                        .Orientation(Orientation.Horizontal)
                                )
                        )
                    ),
                new Grid()
                    .Grid_Row(1)
                    .IsVisible(vm, x => x.NoResultsFound)
                    .Children(
                        new TextBlock()
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .FontSize(24)
                            .FontWeight(FontWeight.Bold)
                            .Text("No Results Found")
                    )
            );
}
