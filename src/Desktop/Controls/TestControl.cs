using Avalonia.Controls;
using Generator.Attributes;

namespace Desktop.Controls;

[AvaloniaProperty("Condition", typeof(Control))]
[AvaloniaProperty("True", typeof(string))]
public partial class TestControl : Control
{
    public TestControl() { }

    partial void OnConditionChanged(Control? newValue)
    {
        throw new System.NotImplementedException();
    }

    partial void OnTrueChanged(string newValue)
    {
        throw new System.NotImplementedException();
    }
}
