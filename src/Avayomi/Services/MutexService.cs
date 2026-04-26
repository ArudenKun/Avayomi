using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avayomi.Core.Mutex;
using Avayomi.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Avayomi.Services;

/// <summary>
/// Provides a service that ensures only a single instance of the application runs by acquiring a
/// named mutex at startup.
/// </summary>
/// <remarks>If another instance of the application is already running and holds the mutex, this service will stop
/// the current application during startup. This helps prevent multiple instances from running concurrently in
/// environments where only one instance should be active.</remarks>
public class MutexService : ISingletonDependency
{
    private readonly ILogger<MutexService> _logger;
    private readonly MutexOptions _options;
    private readonly IAbpHostEnvironment _abpHostEnvironment;
    private readonly ISettingsService _settingsService;
    private readonly IAbpApplicationWithInternalServiceProvider _abpApplication;

    private IMutex? _mutex;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Initializes a new instance of the <see cref="MutexService"/> class.
    /// </summary>
    /// <param name="logger">The logger used to record diagnostic and operational information.</param>
    /// <param name="options">The configuration for creating and identifying the application mutex.</param>
    /// <param name="abpHostEnvironment">The hosting environment information.</param>
    /// <param name="settingsService">The application settings service.</param>
    /// <param name="abpApplication"></param>
    public MutexService(
        ILogger<MutexService> logger,
        IOptions<MutexOptions> options,
        IAbpHostEnvironment abpHostEnvironment,
        ISettingsService settingsService,
        IAbpApplicationWithInternalServiceProvider abpApplication
    )
    {
        _logger = logger;
        _abpHostEnvironment = abpHostEnvironment;
        _settingsService = settingsService;
        _abpApplication = abpApplication;
        _options = options.Value;
    }

    /// <summary>
    /// Initializes the mutex and starts the application, handling scenarios where another
    /// instance may already be running.
    /// </summary>
    public void Start()
    {
        if (_options.UseFileLock)
        {
            _mutex = FileLockMutex.Create(
                _logger,
                Path.Combine(_options.BasePath, $"{_options.MutexId}.lock"),
                _options.ApplicationName
            );
        }
        else
        {
            _mutex = ResourceMutex.Create(
                _logger,
                _options.MutexId,
                _options.ApplicationName,
                _options.IsGlobal
            );
        }

        if (!_mutex.IsLocked)
        {
            NotifyPrimaryInstance();

            _options.WhenNotFirstInstance?.Invoke(_abpHostEnvironment, _logger);
            _logger.LogDebug(
                "Application {applicationName} already running, stopping application.",
                _options.ApplicationName
            );

            _settingsService.DisableSave = true;
            _abpApplication.Shutdown();
            Environment.Exit(0);
        }
        else
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => StartIpcListenerAsync(_options.MutexId, _cts.Token))
                .SafeFireAndForget(ex => _logger.LogError(ex, "Error in IPC Listener."));
        }
    }

    /// <summary>
    /// Attempts to stop the operation asynchronously, honoring cancellation requests.
    /// </summary>
    public void Stop()
    {
        if (_mutex is { IsLocked: true })
        {
            _logger.LogInformation(
                "{App} has been stopped, closing mutex.",
                _options.ApplicationName
            );
        }

        _cts?.Cancel();
        _mutex?.Dispose();
        _mutex = null;
    }

    private void NotifyPrimaryInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _options.MutexId, PipeDirection.Out);
            client.Connect(500); // 500ms timeout
            using var writer = new StreamWriter(client);
            writer.AutoFlush = true;
            writer.WriteLine("RESTORE_WINDOW");
        }
        catch (TimeoutException)
        {
            _logger.LogWarning(
                "Timeout while attempting to connect to the primary instance's named pipe."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send restore signal to the primary instance.");
        }
    }

    private async Task StartIpcListenerAsync(string pipeName, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Instantiate server inside the loop so it can accept consecutive connections
                await using var server = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous
                );

                await server.WaitForConnectionAsync(cancellationToken);

                using var reader = new StreamReader(server);
                var message = await reader.ReadLineAsync();

                if (message == "RESTORE_WINDOW")
                {
                    // Use Post instead of Invoke so we don't block the background listener
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (
                            Application.Current?.ApplicationLifetime
                            is not IClassicDesktopStyleApplicationLifetime desktop
                        )
                            return;

                        if (desktop.MainWindow is not { } mainWindow)
                            return;

                        mainWindow.Show();

                        // Ensure window is brought to front if minimized
                        if (mainWindow.WindowState == WindowState.Minimized)
                        {
                            mainWindow.WindowState = WindowState.Normal;
                        }

                        mainWindow.Activate();
                        mainWindow.Topmost = true;
                        mainWindow.Topmost = false; // Focus hack for strict OS environments
                    });
                }

                // REMOVED: break;
                // We want this loop to continue listening for future instances.
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in named pipe IPC listener.");
        }
    }
}
