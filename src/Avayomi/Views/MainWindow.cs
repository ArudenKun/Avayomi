using System;
using Avalonia;
using Avayomi.Core;
using Avayomi.Services.Settings;
using Avayomi.Settings;
using Microsoft.Extensions.DependencyInjection;
using PleasantUI.Controls;
using PleasantUI.Controls.Chrome;
using R3;
using Volo.Abp.DependencyInjection;

namespace Avayomi.Views;

public sealed class MainWindow : PleasantWindow, ISingletonDependency
{
    private readonly ISettingsService _settingsService;

    private readonly IDisposable _subscription;

    public MainWindow(ISettingsService settingsService, IServiceProvider serviceProvider)
    {
        _settingsService = settingsService;
        Title = AvayomiCoreConsts.Name;
        TitleBarType = PleasantTitleBar.Type.ClassicExtended;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowState = settingsService.Get<AppearanceSettings>().LastWindowState;
        ExtendsContentIntoTitleBar = true;
        Icon = AvaloniaResources.logo_ico.AsWindowIcon();
        _subscription = this.GetObservable(WindowStateProperty).ToObservable().Subscribe(Handler);

        SplashScreen = ActivatorUtilities.CreateInstance<SplashScreen>(serviceProvider);

        Content = ViewFactory.Create<ShellView>();
    }

    private void Handler(WindowState newWindowState)
    {
        _settingsService.Get<AppearanceSettings>().LastWindowState = newWindowState;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        _subscription.Dispose();
    }
}
