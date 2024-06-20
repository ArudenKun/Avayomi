using LucideAvalonia;
using LucideAvalonia.Enum;

namespace Avayomi.ViewModels.Abstractions;

public abstract class PageViewModelBase : BaseViewModel
{
    public abstract int Index { get; }
    public string PageName => GetType().Name.Replace("ViewModel", "");
    public virtual Lucide Icon => new() { Icon = LucideIconNames.Home };
}
