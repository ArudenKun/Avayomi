using Avayomi.ViewModels.Abstractions;
using Material.Icons;

namespace Avayomi.ViewModels.Pages;

public class DownloadsPageViewModel : BasePageViewModel
{
    public override int Index => 4;
    public override MaterialIconKind Icon => MaterialIconKind.Download;
}