using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avayomi.ViewModels;
using SukiUI.Controls;

namespace Avayomi.Views;

public partial class MainView : UserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();

        SideMenu.SelectionChanged += SideMenuOnSelectionChanged;
    }

    private void SideMenuOnSelectionChanged(object? sender, SelectionChangedEventArgs e) { }
}
