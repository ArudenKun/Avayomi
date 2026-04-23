using Avalonia.Controls;
using Avalonia.Markup.Declarative;
using Avayomi.ViewModels;

namespace Avayomi.Views;

public partial class MainView : View<MainViewModel>
{
    protected override object Build(MainViewModel vm) => new TextBlock().Text("Main View");
}
