﻿namespace Avayomi.Generators;

internal static class MetadataNames
{
    public const string AppName = nameof(Avayomi);
    public const string Namespace = $"{AppName}.Core";
    public const string DependencyInjectionNamespace = $"{Namespace}.DependencyInjection";
    public const string Control = "Avalonia.Controls.Control";
    public const string Window = "Avalonia.Controls.Window";
    public const string UserControl = "Avalonia.Controls.UserControl";
    public const string ObservableObject = "CommunityToolkit.Mvvm.ComponentModel.ObservableObject";
    public const string NotifyPropertyChanged = "System.ComponentModel.INotifyPropertyChanged";
}
