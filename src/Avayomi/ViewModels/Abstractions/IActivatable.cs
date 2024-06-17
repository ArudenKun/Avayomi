using System.Diagnostics.CodeAnalysis;

namespace Avayomi.ViewModels.Abstractions;

public interface IActivatable
{
    [RequiresUnreferencedCode("Activate()")]
    public void Activate();

    [RequiresUnreferencedCode("Deactivate()")]
    public void Deactivate();
}
