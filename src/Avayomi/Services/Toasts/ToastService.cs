using System;
using System.Collections.Generic;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Avalonia.Controls.Notifications;
using Avayomi.Services.Settings;
using Avayomi.Settings;
using SukiUI.Toasts;
using Volo.Abp.DependencyInjection;

namespace Avayomi.Services.Toasts;

[AutoExtractInterface]
[ExposeServices(typeof(IToastService))]
public sealed class ToastService : IToastService, ISingletonDependency
{
    private readonly ISukiToastManager _manager;
    private readonly AppearanceSettings _appearanceSettings;

    public ToastService(ISukiToastManager manager, ISettingsService settingsService)
    {
        _manager = manager;
        _appearanceSettings = settingsService.Get<AppearanceSettings>();
    }

    public SukiToastBuilder CreateToast() => _manager.CreateToast();

    public SukiToastBuilder CreateSimpleInfoToast() => _manager.CreateSimpleInfoToast();

    /// <summary>
    /// Creates a toast notification with the specified title, content, and buttons.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <param name="autoDismiss"></param>
    /// <param name="buttons"></param>
    /// <returns></returns>
    public SukiToastBuilder CreateToast(
        string? title,
        string content,
        bool autoDismiss,
        params IEnumerable<ToastActionButton> buttons
    )
    {
        var toast = _manager.CreateToast().WithContent(content);

        if (autoDismiss)
        {
            toast.SetCanDismissByClicking(true);
            toast.SetDismissAfter(_appearanceSettings.ToastDuration);
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            toast.SetTitle(title);
        }

        foreach (var actionButton in buttons)
        {
            toast.AddActionButton(
                actionButton.ButtonContent,
                actionButton.OnClicked,
                actionButton.DismissOnClick,
                actionButton.Styles
            );
        }

        return toast;
    }

    public SukiToastBuilder CreateToast(
        string? title,
        string content,
        params IEnumerable<ToastActionButton> buttons
    ) => CreateToast(title, content, true, buttons);

    public SukiToastBuilder CreateToast(
        NotificationType type,
        string? title,
        string content,
        bool autoDismiss,
        params IEnumerable<ToastActionButton> buttons
    )
    {
        var toast = CreateToast(title, content, autoDismiss, buttons);
        toast.SetType(type);
        return toast;
    }

    public SukiToastBuilder CreateToast(
        NotificationType type,
        string? title,
        string content,
        params IEnumerable<ToastActionButton> buttons
    ) => CreateToast(type, title, content, true, buttons);

    public void ShowToast(
        string title,
        string content,
        bool autoDismiss = true,
        params IEnumerable<ToastActionButton> buttons
    ) => CreateToast(title, content, autoDismiss, buttons).Queue();

    public void ShowToast(
        string title,
        string content,
        params IEnumerable<ToastActionButton> buttons
    ) => CreateToast(title, content, true, buttons).Queue();

    public void ShowToast(
        NotificationType type,
        string? title,
        string content,
        bool autoDismiss,
        params IEnumerable<ToastActionButton> buttons
    ) => CreateToast(type, title, content, autoDismiss, buttons).Queue();

    public void ShowToast(
        NotificationType type,
        string? title,
        string content,
        params IEnumerable<ToastActionButton> buttons
    ) => CreateToast(type, title, content, true, buttons).Queue();

    public void ShowExceptionToast(
        string? title,
        string content,
        bool autoDismiss = true,
        params IEnumerable<ToastActionButton> buttons
    ) => CreateToast(NotificationType.Error, title, content, autoDismiss, buttons).Queue();

    public void ShowExceptionToast(
        string? title,
        string content,
        params IEnumerable<ToastActionButton> buttons
    ) => ShowExceptionToast(title, content, true, buttons);

    public void ShowExceptionToast(
        Exception ex,
        string? title = null,
        string? content = null,
        bool autoDismiss = true,
        params IEnumerable<ToastActionButton> buttons
    ) =>
        CreateToast(
                NotificationType.Error,
                title,
                string.IsNullOrWhiteSpace(content)
                    ? ex.Message
                    : $"{content}{Environment.NewLine}{ex.Message}",
                autoDismiss,
                buttons
            )
            .Queue();
}
