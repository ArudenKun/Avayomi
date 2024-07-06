using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Generator.Interfaces;

namespace Desktop.ViewModels.Common;

[ObservableRecipient]
[SuppressMessage(
    "Trimming",
    "IL2046:\'RequiresUnreferencedCodeAttribute\' annotations must match across all interface implementations or overrides."
)]
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

    protected virtual void OnLoaded() { }

    protected virtual void OnUnloaded() { }
}
