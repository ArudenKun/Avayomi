using System;
using System.Threading.Tasks;
using AsyncNavigation.Abstractions;
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
public abstract partial class ViewModel : ObservableValidator, IDisposable, ITransientDependency
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

    // protected IToastService ToastService => ServiceProvider.GetRequiredService<IToastService>();

    // protected IDialogService DialogService => ServiceProvider.GetRequiredService<IDialogService>();

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
            // ToastService.ShowExceptionToast(ex, "Error", ex.ToStringDemystified());
        }

        return shouldCatch;
    }

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
