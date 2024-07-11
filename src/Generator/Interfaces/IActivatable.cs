using Generator.Metadata.CopyCode;

namespace Generator.Interfaces;

[Copy]
public interface IActivatable
{
    public void Activate();

    public void Deactivate();
}
