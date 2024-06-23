using Avayomi.ViewModels.Abstractions;
using Material.Icons;

namespace Avayomi.ViewModels.Pages;

public class LibraryPageViewModel : BasePageViewModel
{
    public override int Index => 1;
    public override MaterialIconKind Icon => MaterialIconKind.Library;
}