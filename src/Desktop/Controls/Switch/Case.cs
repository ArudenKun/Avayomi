using Avalonia.Controls;
using Generator.Attributes;

namespace Desktop.Controls.Switch;

[AvaloniaProperty("IsDefault", typeof(bool))]
[AvaloniaProperty("Value", typeof(object))]
public partial class Case : ContentControl;
