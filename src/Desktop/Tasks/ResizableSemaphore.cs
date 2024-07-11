using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Desktop.Tasks;

public sealed class ResizableSemaphore
{
    private readonly CancellationTokenSource _cts = new();
    private readonly object _lock = new();
    private readonly Queue<TaskCompletionSource> _waiters = new();
    private int _count;

    private bool _isDisposed;
    private int _maxCount = int.MaxValue;

    public bool IsBusy => MaxCount > 0;

    public int MaxCount
    {
        get
        {
            lock (_lock)
            {
                return _maxCount;
            }
        }
        set
        {
            lock (_lock)
            {
                _maxCount = value;
                Refresh();
            }
        }
    }

    public IDisposable Acquire()
    {
        return AcquireAsync().GetAwaiter().GetResult();
    }

    public async ValueTask<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().Name);

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

    private void Refresh()
    {
        lock (_lock)
        {
            while (_count < MaxCount && _waiters.TryDequeue(out var waiter))
                // Don't increment if the waiter has ben canceled
                if (waiter.TrySetResult())
                    _count++;
        }
    }

    public void Dispose()
    {
        _isDisposed = true;
        _cts.Cancel();
        _cts.Dispose();
    }

    private class AcquiredAccess(ResizableSemaphore semaphore) : IDisposable
    {
        public void Dispose()
        {
            semaphore.Release();
        }
    }
}
