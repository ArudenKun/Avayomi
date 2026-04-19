using System;
using AsyncNavigation;
using AsyncNavigation.Abstractions;
using SukiUI.Controls;

namespace Avayomi.Views;

public sealed class SukiTransitioningContentRegionAdapter
    : RegionAdapterBase<SukiTransitioningContentControl>
{
    public override IRegion CreateRegion(
        string name,
        SukiTransitioningContentControl control,
        IServiceProvider serviceProvider,
        bool? useCache
    )
    {
        return new SukiTransitioningContentRegion(name, control, serviceProvider, useCache);
    }
}
