using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Desktop.Hosting.Ui;

public class UiThread : BaseUiThread<UiContext>
{
    private readonly AppBuilder _appBuilder;

    public UiThread(
        IHostApplicationLifetime hostApplicationLifetime,
        UiContext uiContext,
        AppBuilder appBuilder,
        ILogger<UiThread>? logger
    )
        : base(hostApplicationLifetime, uiContext, logger)
    {
        _appBuilder = appBuilder;
    }

    protected override void UiThreadStart()
    {
        try
        {
            Logger.LogDebug("Started Ui Thread");
            _appBuilder
                .LogToTrace()
                .UsePlatformDetect()
                .WithInterFont()
                .StartWithClassicDesktopLifetime([]);

            var context = new AvaloniaSynchronizationContext(
                Dispatcher.UIThread,
                DispatcherPriority.Default
            );
            SynchronizationContext.SetSynchronizationContext(context);

            UiContext.Dispatcher = Dispatcher.UIThread;
            UiContext.Application = Application.Current;
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "An error occured in the UiThread");
        }

        /*
         * TODO: here we can add code that initializes the UI before the
         * main window is created and activated For example: unhandled
         * exception handlers, maybe instancing, activation, etc...
         */

        // NOTE: First window creation is to be handled in Application.OnFrameworkInitializationCompleted()
    }

    public override Task StopUiThreadAsync()
    {
        Debug.Assert(
            UiContext.Application is not null,
            "Expecting the `Application` in the context to not be null."
        );

        TaskCompletionSource completion = new();
        UiContext.Dispatcher?.Invoke(() =>
        {
            if (
                UiContext.Application.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop
            )
            {
                desktop.Shutdown();
            }

            completion.SetResult();
        });

        return completion.Task;
    }
}
