using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Desktop.Models;
using Desktop.Services.Settings;
using Generator.Interfaces;

namespace Desktop.ViewModels.Common;

[ObservableRecipient]
public abstract partial class BaseViewModel : ObservableValidator, IActivatable
{
    public ISettingsProvider<AppSettings> SettingsProvider { get; } =
        Ioc.Default.GetRequiredService<ISettingsProvider<AppSettings>>();

    public AppSettings AppSettings => SettingsProvider.Value;

    [RequiresUnreferencedCode("Activate()")]
#pragma warning disable IL2046
    public void Activate()
#pragma warning restore IL2046
    {
        Messenger.RegisterAll(this);
        OnLoaded();
    }

    [RequiresUnreferencedCode("Deactivate()")]
#pragma warning disable IL2046
    public void Deactivate()
#pragma warning restore IL2046
    {
        Messenger.UnregisterAll(this);
        OnUnloaded();
    }

    protected virtual void OnLoaded() { }

    protected virtual void OnUnloaded() { }
}
