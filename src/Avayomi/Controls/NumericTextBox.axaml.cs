using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using KeyEventArgs = Avalonia.Input.KeyEventArgs;
using TextBox = Avalonia.Controls.TextBox;

namespace Avayomi.Controls;

/// <summary>
/// Represents a control that can permit only numbers
/// </summary>
[PseudoClasses(":incorrecttext")]
public class NumericTextBox : TextBox
{
    public NumericTextBox()
    {
        InnerRightContent = null;

        PropertyChanged += OnNumberInfoChanged;
    }

    /// <summary>
    /// Determines the <see cref="NumberFormatInfo"/> styled property
    /// </summary>
    public static readonly StyledProperty<NumberFormatInfo?> NumberFormatInfoProperty =
        AvaloniaProperty.Register<NumericTextBox, NumberFormatInfo?>(nameof(NumberFormatInfo));

    /// <summary>
    /// Determines the <see cref="NumberStyle"/> styled property. The default value is <see cref="NumberStyles.Any"/>
    /// </summary>
    public static readonly StyledProperty<NumberStyles> NumberStyleProperty =
        AvaloniaProperty.Register<NumericTextBox, NumberStyles>(
            nameof(NumberStyle),
            defaultValue: NumberStyles.Any
        );

    /// <summary>
    /// Defines the incorrect input occured event
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> IncorrectInputOccuredEvent =
        RoutedEvent.Register<NumericTextBox, RoutedEventArgs>(
            nameof(IncorrectInputOccured),
            RoutingStrategies.Direct
        );

    /// <summary>
    /// Defines the <see cref="HasIncorrectValue"/>
    /// </summary>
    public static readonly DirectProperty<NumericTextBox, bool> HasIncorrectValueProperty =
        AvaloniaProperty.RegisterDirect<NumericTextBox, bool>(
            nameof(HasIncorrectValue),
            o => o.HasIncorrectValue,
            (o, v) => o.HasIncorrectValue = v
        );

    /// <summary>
    /// Gets or sets a value indicating whether the current instance has an incorrect value
    /// </summary>
    public bool HasIncorrectValue
    {
        get;
        set => SetAndRaise(HasIncorrectValueProperty, ref field, value);
    } = false;

    /// <summary>
    /// Gets or sets the <see cref="System.Globalization.NumberStyles"/> value
    /// </summary>
    public NumberStyles NumberStyle
    {
        get => GetValue(NumberStyleProperty);
        set => SetValue(NumberStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="System.Globalization.NumberFormatInfo"/> value
    /// </summary>
    public NumberFormatInfo? NumberFormatInfo
    {
        get => GetValue(NumberFormatInfoProperty);
        set => SetValue(NumberFormatInfoProperty, value);
    }

    /// <summary>
    /// Raises when an incorrect input has occured
    /// </summary>
    public event EventHandler<RoutedEvent> IncorrectInputOccured
    {
        add => AddHandler(IncorrectInputOccuredEvent, value);
        remove => RemoveHandler(IncorrectInputOccuredEvent, value);
    }

    /// <inheritdoc/>
    protected override void OnTextInput(TextInputEventArgs e)
    {
        var newText = Text is null ? e.Text : Text.Insert(CaretIndex, e.Text ?? string.Empty);

        e.Handled = !CheckInput(newText);

        if (e.Handled)
            OnIncorrectInputOccured();

        base.OnTextInput(e);
    }

    /// <summary>
    /// Invoked when incorrect input occurred
    /// </summary>
    protected virtual void OnIncorrectInputOccured()
    {
        RoutedEventArgs e = new(IncorrectInputOccuredEvent);

        RaiseEvent(e);
    }

    /// <summary>
    /// Checks the input
    /// </summary>
    /// <param name="input">The string to check</param>
    /// <returns>
    /// <c>true</c> if the input can be parsed as a double; otherwise, <c>false</c>
    /// </returns>
    protected virtual bool CheckInput(string? input) =>
        double.TryParse(input, NumberStyle, NumberFormatInfo, out var _);

    /// <inheridoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        AddHandler(TextChangedEvent, OnTextChanged, RoutingStrategies.Bubble);
    }

    /// <inheridoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        RemoveHandler(TextChangedEvent, OnTextChanged);

        base.OnDetachedFromVisualTree(e);
    }

    /// <inheridoc/>
    protected override async void OnKeyDown(KeyEventArgs e)
    {
        var keymap = Application.Current!.PlatformSettings?.HotkeyConfiguration;

        if (keymap is null || !Match(keymap.Paste))
        {
            base.OnKeyDown(e);

            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is null)
            return;

        string? clipboardText;

        try
        {
            clipboardText = await clipboard.TryGetTextAsync();
        }
        catch (Exception)
        {
            clipboardText = null;
        }

        if (clipboardText is null)
            return;

        string newValue = Text?.Insert(CaretIndex, clipboardText) ?? clipboardText;

        if (!CheckInput(newValue))
        {
            OnIncorrectInputOccured();

            return;
        }

        Paste();

        return;

        bool Match(List<KeyGesture> gestures) => gestures.Any(g => g.Matches(e));
    }

    /// <summary>
    /// Handles the <see cref="NumericTextBox.TextChangedEvent"/> event
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">Arguments</param>
    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!ReferenceEquals(this, sender))
            return;

        SetIncorrectState(CheckInput(Text));
    }

    /// <summary>
    /// Handles the <see cref="AvaloniaObject.PropertyChanged"/>
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">Arguments</param>
    private void OnNumberInfoChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != NumberStyleProperty && e.Property != NumberFormatInfoProperty)
            return;

        SetIncorrectState(CheckInput(Text));
    }

    /// <summary>
    /// Sets the ':incorrecttext' pseudo class and <see cref="HasIncorrectValue"/> depending on the parameter value
    /// </summary>
    /// <param name="isCorrect">Value</param>
    private void SetIncorrectState(bool isCorrect)
    {
        PseudoClasses.Set(":incorrecttext", !isCorrect);

        HasIncorrectValue = !isCorrect;
    }
}
