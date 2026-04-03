using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avayomi.Navigation;
using Avayomi.Services;
using Avayomi.Services.Dialogs;
using Avayomi.Services.Settings;
using Avayomi.Services.Toasts;
using Avayomi.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using R3;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace Avayomi.ViewModels;

[PublicAPI]
public abstract partial class ViewModel
    : ObservableValidator,
        IDisposable,
        IHasExtraProperties,
        ITransientDependency
{
    protected ViewModel()
    {
        ExtraProperties = new ExtraPropertyDictionary();
        this.SetDefaultsForExtraProperties();
    }

    public required IServiceProvider ServiceProvider { protected get; init; }
    public required ITransientCachedServiceProvider CachedServiceProvider { protected get; init; }

    protected ILoggerFactory LoggerFactory =>
        CachedServiceProvider.GetRequiredService<ILoggerFactory>();

    protected ILogger Logger =>
        CachedServiceProvider.GetService(LoggerFactory.CreateLogger(GetType().FullName!));

    protected IMessenger Messenger => CachedServiceProvider.GetRequiredService<IMessenger>();

    protected ILocalEventBus LocalEventBus =>
        CachedServiceProvider.GetRequiredService<ILocalEventBus>();

    protected INavigationHostManager NavigationHostManager =>
        ServiceProvider.GetRequiredService<INavigationHostManager>();

    protected ISettingsService SettingsService =>
        ServiceProvider.GetRequiredService<ISettingsService>();

    protected IToastService ToastService => ServiceProvider.GetRequiredService<IToastService>();

    protected IDialogService DialogService => ServiceProvider.GetRequiredService<IDialogService>();

    protected IThemeService ThemeService => ServiceProvider.GetRequiredService<IThemeService>();

    public GeneralSettings GeneralSettings => SettingsService.Get<GeneralSettings>();

    public AppearanceSettings AppearanceSettings => SettingsService.Get<AppearanceSettings>();

    public LoggingSettings LoggingSettings => SettingsService.Get<LoggingSettings>();

    public IStorageProvider StorageProvider =>
        ServiceProvider.GetRequiredService<IStorageProvider>();

    public IClipboard Clipboard => ServiceProvider.GetRequiredService<IClipboard>();
    public ILauncher Launcher => ServiceProvider.GetRequiredService<ILauncher>();

    public ExtraPropertyDictionary ExtraProperties { get; }

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
            ToastService.ShowExceptionToast(ex, "Error", ex.ToStringDemystified());
        }

        return shouldCatch;
    }

    #region Disposal

    private bool _disposed;

    ~ViewModel() => Dispose(false);

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
            var disposables = this.GetProperty("Disposables") as CompositeDisposable;
            disposables?.Dispose();
        }

        _disposed = true;
    }

    #endregion
}
