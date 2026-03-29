using System;
using System.Diagnostics.CodeAnalysis;
using SukiUI.Enums;
using SukiUI.Toasts;

namespace Avayomi.Services.Toasts;

public class ToastActionButton
{
    public required object ButtonContent { get; init; }
    public Action<ISukiToast> OnClicked { get; init; } = _ => { };
    public bool DismissOnClick { get; init; }
    public SukiButtonStyles Styles { get; init; } = SukiButtonStyles.Flat;

    public ToastActionButton() { }

    [SetsRequiredMembers]
    public ToastActionButton(
        object buttonContent,
        Action<ISukiToast>? onClicked = null,
        bool dismissOnClick = false,
        SukiButtonStyles styles = SukiButtonStyles.Flat
    )
    {
        ButtonContent = buttonContent;
        if (onClicked is not null)
        {
            OnClicked = onClicked;
        }

        DismissOnClick = dismissOnClick;
        Styles = styles;
    }
}
