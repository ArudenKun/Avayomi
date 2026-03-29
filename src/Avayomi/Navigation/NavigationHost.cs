using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace Avayomi.Navigation;

/// <summary>
///     A control that provides navigation services for Avalonia applications.
///     Displays different views dynamically.
/// </summary>
public class NavigationHost : TemplatedControl, INavigationHost
{
    /// <summary>
    ///     Defines the <see cref="CurrentContent" /> property.
    /// </summary>
    public static readonly StyledProperty<Control?> CurrentContentProperty =
        AvaloniaProperty.Register<NavigationHost, Control?>(nameof(CurrentContent));

    /// <summary>
    ///     Defines the <see cref="DefaultContent" /> property.
    /// </summary>
    public static readonly StyledProperty<Control?> DefaultContentProperty =
        AvaloniaProperty.Register<NavigationHost, Control?>(nameof(DefaultContent));

    private ContentControl? _contentPresenter;

    static NavigationHost()
    {
        DefaultContentProperty.Changed.AddClassHandler<NavigationHost>(
            (host, e) =>
            {
                if (host.CurrentContent == null && e.NewValue is Control defaultContent)
                    host.CurrentContent = defaultContent;
            }
        );
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NavigationHost" /> class.
    /// </summary>
    public NavigationHost() { }

    /// <summary>
    ///     Gets or sets the default content to display when no content is set.
    /// </summary>
    public Control? DefaultContent
    {
        get => GetValue(DefaultContentProperty);
        set => SetValue(DefaultContentProperty, value);
    }

    /// <summary>
    ///     Gets or sets the current content being displayed.
    /// </summary>
    [Content]
    public Control? CurrentContent
    {
        get => GetValue(CurrentContentProperty);
        set => SetValue(CurrentContentProperty, value);
    }

    /// <summary>
    ///     Gets the current content as object (INavigationHost implementation).
    /// </summary>
    object? INavigationHost.CurrentContent => CurrentContent;

    /// <summary>
    ///     Sets the content (INavigationHost implementation).
    /// </summary>
    void INavigationHost.SetContent(object content)
    {
        if (content is Control control)
        {
            Navigate(control);
        }
        else
        {
            throw new ArgumentException("Content must be an Avalonia Control", nameof(content));
        }
    }

    /// <summary>
    ///     Navigates to the specified content.
    /// </summary>
    /// <param name="content">The content to navigate to.</param>
    public void Navigate(Control content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        CurrentContent = content;
        UpdateContentPresenter();
    }

    /// <summary>
    ///     Called when the control template is applied.
    /// </summary>
    /// <param name="e">The template applied event arguments.</param>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _contentPresenter = e.NameScope.Find<ContentControl>("PART_ContentPresenter");

        if (_contentPresenter != null && CurrentContent != null)
            _contentPresenter.Content = CurrentContent;
    }

    private void UpdateContentPresenter()
    {
        if (_contentPresenter != null)
            _contentPresenter.Content = CurrentContent;
    }
}
