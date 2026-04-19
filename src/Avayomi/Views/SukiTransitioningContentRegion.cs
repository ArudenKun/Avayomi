using System;
using AsyncNavigation;
using AsyncNavigation.Core;
using Avalonia.Controls;
using Avalonia.Data;
using SukiUI.Controls;

namespace Avayomi.Views;

public class SukiTransitioningContentRegion
    : RegionBase<SukiTransitioningContentRegion, SukiTransitioningContentControl>
{
    public SukiTransitioningContentRegion(
        string name,
        SukiTransitioningContentControl contentControl,
        IServiceProvider serviceProvider,
        bool? useCache
    )
        : base(name, contentControl, serviceProvider)
    {
        EnableViewCache = useCache ?? true;
        IsSinglePageRegion = true;
    }

    public override NavigationPipelineMode NavigationPipelineMode
    {
        get => NavigationPipelineMode.RenderFirst;
    }

    protected override void InitializeOnRegionCreated(SukiTransitioningContentControl control)
    {
        base.InitializeOnRegionCreated(control);
        control.Tag = this;
        control.Bind(
            SukiTransitioningContentControl.ContentProperty,
            new Binding(nameof(RegionContext.Selected))
            {
                Source = _context.Selected?.IndicatorHost.Value?.Host as Control,
                Mode = BindingMode.TwoWay,
            }
        );
    }

    public override void Dispose()
    {
        base.Dispose();
        _context.Selected = null;
        RegionControlAccessor.ExecuteOn(control =>
        {
            control.Content = null;
        });
    }

    public override void ProcessActivate(NavigationContext navigationContext)
    {
        _context.Selected = navigationContext;
    }

    public override void ProcessDeactivate(NavigationContext? navigationContext)
    {
        _context.Selected = null;
    }
}
