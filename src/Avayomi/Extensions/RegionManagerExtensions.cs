using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using AsyncNavigation.Abstractions;
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
