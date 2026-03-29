using JetBrains.Annotations;

namespace Avayomi.Core.Tasks;

[PublicAPI]
public sealed class ResizableSemaphore : IDisposable
{
    private readonly Lock _lock = new();
    private readonly Queue<TaskCompletionSource> _waiters = new();
    private readonly CancellationTokenSource _cts = new();

    private bool _isDisposed;
    private int _count;

    public bool IsBusy => MaxCount > 0;

    public int MaxCount
    {
        get
        {
            lock (_lock)
            {
                return field;
            }
        }
        set
        {
            lock (_lock)
            {
                field = value;
                Refresh();
            }
        }
    } = int.MaxValue;

    public IDisposable Acquire() => AcquireAsync().GetAwaiter().GetResult();

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var waiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (_cts.Token.Register(() => waiter.TrySetCanceled(_cts.Token)))
        await using (cancellationToken.Register(() => waiter.TrySetCanceled(cancellationToken)))
        {
            lock (_lock)
            {
                _waiters.Enqueue(waiter);
                Refresh();
            }

            await waiter.Task;
            return new AcquiredAccess(this);
        }
    }

    public void Release()
    {
        lock (_lock)
        {
            _count--;
            Refresh();
        }
    }

    public void Dispose()
    {
        _isDisposed = true;
        _cts.Cancel();
        _cts.Dispose();
    }

    private void Refresh()
    {
        lock (_lock)
        {
            while (_count < MaxCount && _waiters.TryDequeue(out var waiter))
            {
                // Don't increment if the waiter has ben canceled
                if (waiter.TrySetResult())
                    _count++;
            }
        }
    }

    private sealed class AcquiredAccess(ResizableSemaphore semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}
