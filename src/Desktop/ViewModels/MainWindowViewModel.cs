using System.Collections.Generic;
using System.Linq;
using Desktop.ViewModels.Common;
using Generator.Attributes;

namespace Desktop.ViewModels;

[Singleton]
public sealed class MainWindowViewModel : BaseViewModel
{
    public MainWindowViewModel(IEnumerable<BasePageViewModel> pages)
    {
        Pages = new List<BasePageViewModel>(pages).OrderBy(x => x.Index).ToArray();
    }

    public IReadOnlyCollection<BasePageViewModel> Pages { get; }
}
