using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Generator.Attributes;
using Generator.Interfaces;

namespace Desktop;

[StaticViewLocator]
public sealed partial class ViewLocator
{
    [RequiresUnreferencedCode("Calls RegisterActivatable(ViewModel, Control)")]
#pragma warning disable IL2046
    public Control? Build(object? viewModel)
#pragma warning restore IL2046
    {
        if (viewModel is null)
            return null;

        var type = viewModel.GetType();
        var name = type.FullName;

        if (!ViewMap.TryGetValue(type, out var factory))
            return new TextBlock { Text = "Not Found: " + name };

        var control = factory();
        control.DataContext = viewModel;
        RegisterActivatable(viewModel, control);
        return control;
    }

    public bool Match(object? data) => data is INotifyPropertyChanged;

    [RequiresUnreferencedCode("Calls Loaded(Object, RoutedEventArgs)")]
    private static void RegisterActivatable(object viewModel, Control control)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (viewModel is not IActivatable activatableViewModel)
        {
            return;
        }

        control = control ?? throw new ArgumentNullException(nameof(control));

        control.Loaded += Loaded;
        control.Unloaded += Unloaded;
        return;

        [RequiresUnreferencedCode("IActivatable.Activate()")]
        void Loaded(object? sender, RoutedEventArgs e)
        {
            activatableViewModel?.Activate();
        }

        [RequiresUnreferencedCode("IActivatable.Deactivate()")]
        void Unloaded(object? sender, RoutedEventArgs e)
        {
            activatableViewModel?.Deactivate();

            control.Loaded -= Loaded;
            control.Unloaded -= Unloaded;
        }
    }
}
