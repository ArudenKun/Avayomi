using Avalonia.Data;
using Avayomi.ViewModels;
using Avayomi.Views.Pages;
using PleasantUI;
using PleasantUI.Controls;

namespace Avayomi.Views;

public sealed class MainView : View<MainViewModel>
{
    protected override object Build(MainViewModel vm) =>
        new Panel().Children(
            new NavigationView()
                .SelectedItem(vm, x => x.NavigationViewItem, BindingMode.TwoWay)
                .Items(
                    NavigationViewItem()
                        .Header("Anime")
                        .Icon(MaterialIcons.Book)
                        .Content(ViewFactory.Create<AnimePageView>()),
                    NavigationViewItem()
                        .DockPanel_Dock(Dock.Bottom)
                        .Header("About")
                        .Icon(MaterialIcons.InformationOutline)
                        .Content(ViewFactory.Create<AboutPageView>()),
                    NavigationViewItem()
                        .DockPanel_Dock(Dock.Bottom)
                        .Header("Settings")
                        .Icon(MaterialIcons.CogOutline)
                        .Content(ViewFactory.Create<SettingsPageView>())
                )
        );

    private static NavigationViewItem NavigationViewItem() => new();
}
