using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AsyncNavigation.Abstractions;
using AsyncNavigation.Avalonia;
using AsyncNavigation.Core;
using Avayomi.Utilities;
using Avayomi.Views;

namespace Avayomi.Extensions;

public static class RegionManagerExtensions
{
    public static T RegionManager_RegionName<T>(
        this T control,
        string regionName,
        [CallerFilePath] string? callerFile = null,
        [CallerLineNumber] int callerLine = 0
    )
        where T : Control =>
        control._set(
            () => RegionManager.SetRegionName(control, regionName),
            callerFile,
            callerLine
        );

    public static T RegionManager_PreferCache<T>(
        this T control,
        bool value,
        [CallerFilePath] string? callerFile = null,
        [CallerLineNumber] int callerLine = 0
    )
        where T : Control =>
        control._set(() => RegionManager.SetPreferCache(control, value), callerFile, callerLine);

    public static void RequestNavigate<TView>(
        this IRegionManager regionManager,
        string regionName,
        INavigationParameters? navigationParameters = null,
        bool replay = false,
        TimeSpan? delay = null,
        Action<NavigationResult>? onCompleted = null,
        CancellationToken cancellationToken = default
    )
        where TView : class, IView =>
        regionManager.RequestNavigate(
            regionName,
            typeof(TView),
            navigationParameters,
            replay,
            delay,
            onCompleted,
            cancellationToken
        );

    public static void RequestNavigate(
        this IRegionManager regionManager,
        string regionName,
        Type viewType,
        INavigationParameters? navigationParameters = null,
        bool replay = false,
        TimeSpan? delay = null,
        Action<NavigationResult>? onCompleted = null,
        CancellationToken cancellationToken = default
    ) =>
        DispatchHelper.Post(async void () =>
        {
            if (delay.HasValue)
            {
                await Task.Delay(delay.Value, cancellationToken);
            }

            try
            {
                var result = await regionManager.RequestNavigateAsync(
                    regionName,
                    viewType,
                    navigationParameters,
                    replay,
                    cancellationToken
                );
                onCompleted?.Invoke(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        });

    public static Task<NavigationResult> RequestNavigateAsync<TView>(
        this IRegionManager regionManager,
        string regionName,
        INavigationParameters? navigationParameters = null,
        bool replay = false,
        CancellationToken cancellationToken = default
    )
        where TView : Control, IView =>
        regionManager.RequestNavigateAsync(
            regionName,
            typeof(TView),
            navigationParameters,
            replay,
            cancellationToken
        );

    public static Task<NavigationResult> RequestNavigateAsync(
        this IRegionManager regionManager,
        string regionName,
        Type viewType,
        INavigationParameters? navigationParameters = null,
        bool replay = false,
        CancellationToken cancellationToken = default
    )
    {
        var viewName = viewType.Name;
        if (!viewType.IsAssignableTo<IView>())
        {
            throw new InvalidOperationException($"{viewName} does not implement IView");
        }

        var viewAttribute = viewType.GetSingleAttributeOrNull<ViewAttribute>(false);
        if (viewAttribute is not null)
        {
            viewName = viewAttribute.Name;
        }

        return regionManager.RequestNavigateAsync(
            regionName,
            viewName,
            navigationParameters,
            replay,
            cancellationToken
        );
    }
}
