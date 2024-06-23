using Avayomi.ViewModels.Abstractions;
using Material.Icons;

namespace Avayomi.ViewModels.Pages;

public class BrowsePageViewModel : BasePageViewModel
{
    public override int Index => 3;
    public override MaterialIconKind Icon => MaterialIconKind.Compass;
}