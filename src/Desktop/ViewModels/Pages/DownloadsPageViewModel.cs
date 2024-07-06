using Desktop.ViewModels.Common;
using Material.Icons;

namespace Desktop.ViewModels.Pages;

public class DownloadsPageViewModel : BasePageViewModel
{
    public override int Index => 4;
    public override MaterialIconKind Icon => MaterialIconKind.Download;
}