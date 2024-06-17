using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace Avayomi.ViewModels.Abstractions;

[ObservableRecipient]
public abstract partial class BaseViewModel : ObservableValidator, IActivatable
{
    [RequiresUnreferencedCode("Activate()")]
    public void Activate()
    {
        Messenger.RegisterAll(this);
        OnLoaded();
    }

    [RequiresUnreferencedCode("Deactivate()")]
    public void Deactivate()
    {
        Messenger.UnregisterAll(this);
        OnUnloaded();
    }

    protected virtual void OnLoaded()
    {
    }

    protected virtual void OnUnloaded()
    {
    }
}