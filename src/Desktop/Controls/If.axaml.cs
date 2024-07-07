using Avalonia.Controls;
using DependencyPropertyGenerator;

namespace Desktop.Controls;

[DependencyProperty<bool>("Condition")]
[DependencyProperty<Control>("True")]
[DependencyProperty<Control>("False")]
public partial class If : UserControl
{
    public If()
    {
        InitializeComponent();
    }

    partial void OnConditionChanged() => UpdateContent();

    partial void OnTrueChanged() => UpdateContent();

    partial void OnFalseChanged() => UpdateContent();

    private void UpdateContent() => Content = Condition ? True : False;
}
