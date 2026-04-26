namespace Avayomi.Core.Mutex;

public interface IMutex : IDisposable
{
    bool IsLocked { get; }
    bool Lock();
}
