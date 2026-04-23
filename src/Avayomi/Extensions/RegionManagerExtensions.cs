using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using AsyncNavigation.Abstractions;
using AsyncNavigation.Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Declarative;
using Avalonia.Threading;

namespace Avayomi.Extensions;

public static class RegionManagerExtensions
{
    public static T RegionManager_RegionName<T>(
        this T control,
        string regionName,
        // ReSharper disable InconsistentNaming
        [CallerFilePath] string? _callerFile = null,
        [CallerLineNumber] int _callerLine = 0
    // ReSharper restore InconsistentNaming

    )
        where T : Control =>
        control._set(
            () => RegionManager.SetRegionName(control, regionName),
            _callerFile,
            _callerLine
        );

    public static T RegionManager_PreferCache<T>(
        this T control,
        bool value,
        // ReSharper disable InconsistentNaming
        [CallerFilePath] string? _callerFile = null,
        [CallerLineNumber] int _callerLine = 0
    // ReSharper restore InconsistentNaming

    )
        where T : Control =>
        control._set(() => RegionManager.SetPreferCache(control, value), _callerFile, _callerLine);

    public static void RequestNavigate<TView>(
        this IRegionManager regionManager,
        string regionName,
        INavigationParameters? navigationParameters = null,
        bool replay = false,
        TimeSpan? delay = null,
        CancellationToken cancellationToken = default
    )
        where TView : class, IView =>
        regionManager.RequestNavigate(
            regionName,
            typeof(TView),
            navigationParameters,
            replay,
            delay,
            cancellationToken
        );

    public static void RequestNavigate(
        this IRegionManager regionManager,
        string regionName,
        Type viewType,
        INavigationParameters? navigationParameters = null,
        bool replay = false,
        TimeSpan? delay = null,
        CancellationToken cancellationToken = default
    ) =>
        Dispatcher.UIThread.Post(async void () =>
        {
            if (delay is not null)
            {
                await Task.Delay(delay.Value, cancellationToken);
            }

            regionManager
                .RequestNavigateAsync(
                    regionName,
                    viewType,
                    navigationParameters,
                    replay,
                    cancellationToken
                )
                .SafeFireAndForget(ex => throw ex);
        });

    public static Task RequestNavigateAsync<TView>(
        this IRegionManager regionManager,
        string regionName,
        INavigationParameters? navigationParameters = null,
        bool replay = false,
        CancellationToken cancellationToken = default
    )
        where TView : class, IView =>
        regionManager.RequestNavigateAsync(
            regionName,
            typeof(TView),
            navigationParameters,
            replay,
            cancellationToken
        );

    public static Task RequestNavigateAsync(
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
            throw new InvalidOperationException($"{viewName} is invalid");
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
