using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AsyncNavigation;
using AsyncNavigation.Abstractions;
using AsyncNavigation.Core;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avayomi.Services.Settings;
using Avayomi.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using R3;
using Volo.Abp.DependencyInjection;

namespace Avayomi.ViewModels;

[PublicAPI]
public abstract partial class ViewModel : ObservableValidator, INavigationAware, IDisposable
{
    public required IServiceProvider ServiceProvider { protected get; init; }
    public required ITransientCachedServiceProvider CachedServiceProvider { protected get; init; }

    protected ILoggerFactory LoggerFactory =>
        CachedServiceProvider.GetRequiredService<ILoggerFactory>();

    protected ILogger Logger =>
        CachedServiceProvider.GetService(LoggerFactory.CreateLogger(GetType().FullName!));

    protected IMessenger Messenger => CachedServiceProvider.GetRequiredService<IMessenger>();

    protected IRegionManager RegionManager =>
        CachedServiceProvider.GetRequiredService<IRegionManager>();

    protected ISettingsService SettingsService =>
        ServiceProvider.GetRequiredService<ISettingsService>();

    //protected IToastService ToastService => ServiceProvider.GetRequiredService<IToastService>();

    //protected IDialogService DialogService => ServiceProvider.GetRequiredService<IDialogService>();

    //protected IThemeService ThemeService => ServiceProvider.GetRequiredService<IThemeService>();

    public GeneralSettings GeneralSettings => SettingsService.Get<GeneralSettings>();

    public AppearanceSettings AppearanceSettings => SettingsService.Get<AppearanceSettings>();

    public LoggingSettings LoggingSettings => SettingsService.Get<LoggingSettings>();

    public IStorageProvider StorageProvider =>
        ServiceProvider.GetRequiredService<IStorageProvider>();

    public IClipboard Clipboard => ServiceProvider.GetRequiredService<IClipboard>();
    public ILauncher Launcher => ServiceProvider.GetRequiredService<ILauncher>();

    [ObservableProperty]
    public virtual partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string BusyText { get; set; } = string.Empty;

    public virtual void OnLoaded() { }

    public virtual void OnUnloaded() { }

    protected void OnAllPropertiesChanged() => OnPropertyChanged(string.Empty);

    protected virtual async Task SetBusyAsync(
        Func<Task> func,
        string busyText = "",
        bool showException = true
    )
    {
        IsBusy = true;
        BusyText = busyText;
        try
        {
            await func();
        }
        catch (Exception ex) when (LogException(ex, true, showException))
        {
            // Not Used
        }
        finally
        {
            IsBusy = false;
            BusyText = string.Empty;
        }
    }

    protected bool LogException(Exception? ex, bool shouldCatch = false, bool shouldDisplay = false)
    {
        if (ex is null)
        {
            return shouldCatch;
        }

        // Logger.LogException(ex);
        if (shouldDisplay)
        {
            //ToastService.ShowExceptionToast(ex, "Error", ex.ToStringDemystified());
        }

        return shouldCatch;
    }

    #region Navigation

    /// <inheritdoc/>
    /// <remarks>Called only the first time a view is created and shown. Default implementation does nothing.</remarks>
    public virtual Task InitializeAsync(NavigationContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    /// <remarks>Called every time the view becomes the active view. Default implementation does nothing.</remarks>
    public virtual Task OnNavigatedToAsync(NavigationContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    /// <remarks>Called when navigating away from this view. Default implementation does nothing.</remarks>
    public virtual Task OnNavigatedFromAsync(NavigationContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    /// <remarks>
    /// Controls whether a cached view instance can be reused for the incoming navigation request.
    /// Default returns <see langword="true"/>, meaning the cached instance is always reused.
    /// Override and return <see langword="false"/> to force creation of a new instance.
    /// </remarks>
    public virtual Task<bool> IsNavigationTargetAsync(NavigationContext context) =>
        Task.FromResult(true);

    /// <inheritdoc/>
    /// <remarks>Called when the view is being removed from the region cache. Default implementation does nothing.</remarks>
    public virtual Task OnUnloadAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    /// <remarks>
    /// Raise this event to request that the framework proactively removes this view from the region.
    /// Use <see cref="RequestUnloadAsync"/> as a convenient helper to raise it.
    /// </remarks>
    public event AsyncEventHandler<AsyncEventArgs>? AsyncRequestUnloadEvent;

    /// <summary>
    /// Raises <see cref="AsyncRequestUnloadEvent"/> to request that the framework remove this view.
    /// </summary>
    protected Task RequestUnloadAsync(CancellationToken cancellationToken = default)
    {
        var handler = AsyncRequestUnloadEvent;
        if (handler is not null)
            return handler(this, new AsyncEventArgs(cancellationToken));
        return Task.CompletedTask;
    }

    #endregion

    #region Disposal

    private bool _disposed;

    ~ViewModel() => Dispose(false);

    public CompositeDisposable Disposables { get; } = new();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose"/>>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Disposables.Dispose();
        }

        _disposed = true;
    }

    #endregion
}
