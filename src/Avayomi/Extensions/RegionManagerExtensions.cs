using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using AsyncNavigation.Abstractions;
using AsyncNavigation.Core;
using Avalonia.Threading;

namespace Avayomi.Extensions;

public static class RegionManagerExtensions
{
    public static void RequestNavigate<TView>(
        this IRegionManager regionManager,
        string regionName,
        INavigationParameters? navigationParameters = null,
        bool replay = false,
        TimeSpan? delay = null,
        Action<NavigationResult>? onComplete = null,
        CancellationToken cancellationToken = default
    )
        where TView : class, IView =>
        regionManager.RequestNavigate(
            regionName,
            typeof(TView),
            navigationParameters,
            replay,
            delay,
            onComplete,
            cancellationToken
        );

    public static void RequestNavigate(
        this IRegionManager regionManager,
        string regionName,
        Type viewType,
        INavigationParameters? navigationParameters = null,
        bool replay = false,
        TimeSpan? delay = null,
        Action<NavigationResult>? onComplete = null,
        CancellationToken cancellationToken = default
    ) =>
        Dispatcher.UIThread.Invoke(async void () =>
        {
            if (delay is not null)
            {
                await Task.Delay(delay.Value, cancellationToken);
            }

            var result = await regionManager.RequestNavigateAsync(
                regionName,
                viewType,
                navigationParameters,
                replay,
                cancellationToken
            );

            onComplete?.Invoke(result);
        });

    public static Task<NavigationResult> RequestNavigateAsync<TView>(
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
