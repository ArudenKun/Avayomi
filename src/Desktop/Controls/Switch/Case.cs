using Avalonia.Controls;
using DependencyPropertyGenerator;

namespace Desktop.Controls.Switch;

[DependencyProperty<bool>("IsDefault")]
[DependencyProperty<object>("Value")]
public partial class Case : ContentControl;
