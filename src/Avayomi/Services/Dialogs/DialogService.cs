using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Avalonia.Controls.Notifications;
using Avayomi.ViewModels.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;
using Volo.Abp.DependencyInjection;

namespace Avayomi.Services.Dialogs;

[AutoExtractInterface]
[ExposeServices(typeof(IDialogService))]
public sealed class DialogService : IDialogService, ISingletonDependency
{
    private readonly ISukiDialogManager _manager;

    public DialogService(ISukiDialogManager manager, IServiceProvider serviceProvider)
    {
        _manager = manager;
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public SukiDialogBuilder CreateDialog() => _manager.CreateDialog();

    public SukiDialogBuilder CreateDialog(
        string? title,
        string content,
        bool showBackground,
        bool dismissOnClick,
        Action<ISukiDialog> onDismiss,
        params IEnumerable<DialogActionButton> buttons
    )
    {
        var dialog = _manager.CreateDialog().WithContent(content);
        dialog.SetCanDismissWithBackgroundClick(dismissOnClick);
        dialog.ShowCardBackground(showBackground);
        dialog.OnDismissed(onDismiss);
        foreach (var actionButton in buttons)
        {
            dialog.AddActionButton(
                actionButton.ButtonContent,
                actionButton.OnClicked,
                actionButton.DismissOnClick,
                actionButton.Classes
            );
        }

        return dialog;
    }

    public SukiDialogBuilder CreateMessageBox(
        string title,
        string message,
        NotificationType type,
        bool canDismissWithBackgroundClick
    )
    {
        var builder = _manager.CreateDialog().OfType(type).WithTitle(title).WithContent(message);
        if (canDismissWithBackgroundClick)
        {
            builder.Dismiss().ByClickingBackground();
        }

        return builder;
    }

    public void ShowMessageBox(
        string title,
        string message,
        NotificationType type,
        bool canDismissWithBackgroundClick
    )
    {
        CreateMessageBox(title, message, type, canDismissWithBackgroundClick).TryShow();
    }

    public void ShowInformationMessageBox(
        string title,
        string message,
        bool canDismissWithBackgroundClick = true
    )
    {
        ShowMessageBox(title, message, NotificationType.Information, canDismissWithBackgroundClick);
    }

    public void ShowSuccessMessageBox(
        string title,
        string message,
        bool canDismissWithBackgroundClick = true
    )
    {
        ShowMessageBox(title, message, NotificationType.Success, canDismissWithBackgroundClick);
    }

    public void ShowWarningMessageBox(
        string title,
        string message,
        bool canDismissWithBackgroundClick = true
    )
    {
        ShowMessageBox(title, message, NotificationType.Warning, canDismissWithBackgroundClick);
    }

    public void ShowErrorMessageBox(
        string title,
        string message,
        bool canDismissWithBackgroundClick = true
    )
    {
        ShowMessageBox(title, message, NotificationType.Error, canDismissWithBackgroundClick);
    }

    public void ShowDialog<TViewModel>(TViewModel viewModel)
        where TViewModel : DialogViewModel =>
        _manager
            .CreateDialog()
            .WithViewModel(d =>
            {
                viewModel.SetDialog(d);
                return viewModel;
            })
            .TryShow();

    public void ShowDialog<TViewModel>()
        where TViewModel : DialogViewModel =>
        ShowDialog(ServiceProvider.GetRequiredService<TViewModel>());

    public async Task<TResult?> ShowDialogAsync<TResult>(DialogViewModel<TResult> viewModel)
    {
        var builder = _manager.CreateDialog().WithViewModel(viewModel);
        builder.Completion = viewModel.Completion;
        await builder.TryShowAsync();
        return viewModel.DialogResult;
    }
}
