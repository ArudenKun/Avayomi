using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avayomi.Utilities;

public static class DispatchHelper
{
    /// <inheritdoc cref="Dispatcher.Invoke(Action, DispatcherPriority)"/>>
    public static void Invoke(Action callback, DispatcherPriority? priority = null)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            callback();
            return;
        }
        Dispatcher.UIThread.Invoke(callback, priority ?? DispatcherPriority.Send);
    }

    /// <inheritdoc cref="Dispatcher.Invoke{TResult}(Func{TResult}, DispatcherPriority)"/>>
    public static TResult Invoke<TResult>(
        Func<TResult> callback,
        DispatcherPriority? priority = null
    ) =>
        Dispatcher.UIThread.CheckAccess()
            ? callback()
            : Dispatcher.UIThread.Invoke(callback, priority ?? DispatcherPriority.Send);

    /// <inheritdoc cref="Dispatcher.InvokeAsync(Func{Task}, DispatcherPriority)"/>>
    public static async Task InvokeAsync(Func<Task> func, DispatcherPriority? priority = null)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            await func();
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(func, priority ?? DispatcherPriority.Default);
    }

    /// <inheritdoc cref="Dispatcher.InvokeAsync{TResult}(Func{Task{TResult}}, DispatcherPriority)"/>
    public static async Task<TResult> InvokeAsync<TResult>(
        Func<Task<TResult>> callback,
        DispatcherPriority? priority = null
    )
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return await callback();
        }

        return await Dispatcher.UIThread.InvokeAsync(
            callback,
            priority ?? DispatcherPriority.Default
        );
    }

    /// <inheritdoc cref="Dispatcher.Post(Action, DispatcherPriority)"/>>
    public static void Post(Action callback, DispatcherPriority priority = default)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            callback();
            return;
        }
        Dispatcher.UIThread.Post(callback, priority);
    }

    /// <inheritdoc cref="Dispatcher.Post(Action,DispatcherPriority)"/>>
    public static async Task PostAsync(Func<Task> callback) =>
        await Dispatcher.UIThread.PostAsync(callback);

    public static void RunTimer(
        Func<bool> callback,
        TimeSpan interval,
        DispatcherPriority priority = default
    ) => DispatcherTimer.Run(callback, interval, priority);

    public static void RunTimerOnce(
        Action callback,
        TimeSpan interval,
        DispatcherPriority priority = default
    ) => DispatcherTimer.RunOnce(callback, interval, priority);

    extension(IDispatcher dispatcher)
    {
        public Task PostAsync(Func<Task> action, DispatcherPriority dispatcherPriority = default)
        {
            var tcs = new TaskCompletionSource();
            dispatcher.Post(
                () =>
                {
                    action().GetAwaiter().GetResult();
                    tcs.SetResult();
                },
                dispatcherPriority
            );
            return tcs.Task;
        }

        public Task<T> PostAsync<T>(
            Func<Task<T>> func,
            DispatcherPriority dispatcherPriority = default
        )
        {
            var tcs = new TaskCompletionSource<T>();
            dispatcher.Post(
                () =>
                {
                    var value = func().GetAwaiter().GetResult();
                    tcs.SetResult(value);
                },
                dispatcherPriority
            );
            return tcs.Task;
        }
    }

    extension(Task task)
    {
        public void WaitOnDispatcherFrame(Dispatcher? dispatcher = null)
        {
            var frame = new DispatcherFrame();
            AggregateException? capturedException = null;

            task.ContinueWith(
                t =>
                {
                    capturedException = t.Exception;
                    frame.Continue = false;
                },
                TaskContinuationOptions.AttachedToParent
            );

            dispatcher ??= Dispatcher.UIThread;
            dispatcher.PushFrame(frame);

            if (capturedException != null)
            {
                throw capturedException;
            }
        }
    }
}
