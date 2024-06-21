using System.Collections.Generic;
using Avayomi.Core.Attributes;
using Avayomi.ViewModels.Abstractions;
using CommunityToolkit.Mvvm.Input;
using ZiggyCreatures.Caching.Fusion;

namespace Avayomi.ViewModels;

[Singleton]
public sealed partial class MainWindowViewModel : BaseViewModel
{
    private readonly IFusionCache _fusionCache;

    public MainWindowViewModel(IFusionCache fusionCache)
    {
        _fusionCache = fusionCache;
    }

    [RelayCommand]
    private void ShuwtDown()
    {
        var a = _fusionCache.GetOrSet("key", new Dictionary<string, string> { ["hello"] = "YEEEET" });
    }
}