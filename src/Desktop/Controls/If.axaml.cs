using Avalonia.Controls;
using Generator.Attributes;

namespace Desktop.Controls;

// [AvaloniaProperty<bool>("Condition")]
// [AvaloniaProperty<Control>("True")]
// [AvaloniaProperty<Control>("False")]
[AvaloniaProperty("Condition", typeof(bool))]
[AvaloniaProperty("True", typeof(Control))]
[AvaloniaProperty("False", typeof(Control))]
public partial class If : UserControl
{
    public If()
    {
        InitializeComponent();
    }

    partial void OnConditionChanged()
    {
        UpdateContent();
    }

    partial void OnTrueChanged()
    {
        UpdateContent();
    }

    partial void OnFalseChanged()
    {
        UpdateContent();
    }

    private void UpdateContent()
    {
        Content = Condition ? True : False;
    }
}
