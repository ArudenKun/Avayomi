
using Material.Icons;

namespace Avayomi.ViewModels.Abstractions;

public abstract class BasePageViewModel : BaseViewModel
{
    public abstract int Index { get; }
    public virtual string DisplayName => GetType().Name.Replace("PageViewModel", "");
    public virtual MaterialIconKind Icon => MaterialIconKind.Home;

}