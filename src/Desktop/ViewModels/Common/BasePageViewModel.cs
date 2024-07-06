
using Material.Icons;

namespace Desktop.ViewModels.Common;

public abstract class BasePageViewModel : BaseViewModel
{
    public abstract int Index { get; }
    public virtual string DisplayName => GetType().Name.Replace("PageViewModel", "");
    public virtual MaterialIconKind Icon => MaterialIconKind.Home;

}