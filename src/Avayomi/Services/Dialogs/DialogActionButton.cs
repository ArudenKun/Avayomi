// using System;
// using System.Diagnostics.CodeAnalysis;
// using SukiUI.Dialogs;
//
// namespace Avayomi.Services.Dialogs;
//
// public class DialogActionButton
// {
//     public required object ButtonContent { get; init; }
//     public Action<ISukiDialog> OnClicked { get; init; } = _ => { };
//     public bool DismissOnClick { get; init; }
//     public string[] Classes { get; init; } = [];
//
//     public DialogActionButton() { }
//
//     [SetsRequiredMembers]
//     public DialogActionButton(
//         object buttonContent,
//         Action<ISukiDialog>? onClicked = null,
//         bool dismissOnClick = false,
//         string[]? classes = null
//     )
//     {
//         ButtonContent = buttonContent;
//         if (onClicked is not null)
//         {
//             OnClicked = onClicked;
//         }
//
//         DismissOnClick = dismissOnClick;
//         Classes = classes ?? [];
//     }
// }
