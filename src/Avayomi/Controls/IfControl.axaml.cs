using Avalonia;
using Avalonia.Controls;

namespace Avayomi.Controls;

public partial class IfControl : UserControl
{
    public IfControl()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<bool> ConditionProperty = AvaloniaProperty.Register<
        IfControl,
        bool
    >(nameof(Condition));

    public static readonly StyledProperty<Control> TrueProperty = AvaloniaProperty.Register<
        IfControl,
        Control
    >(nameof(True));

    public static readonly StyledProperty<Control> FalseProperty = AvaloniaProperty.Register<
        IfControl,
        Control
    >(nameof(False));

    public bool Condition
    {
        get => GetValue(ConditionProperty);
        set => SetValue(ConditionProperty, value);
    }

    public Control True
    {
        get => GetValue(TrueProperty);
        set => SetValue(TrueProperty, value);
    }

    public Control False
    {
        get => GetValue(FalseProperty);
        set => SetValue(FalseProperty, value);
    }

    static IfControl()
    {
        ConditionProperty.Changed.AddClassHandler<IfControl>(ConditionPropertyChanged);
        TrueProperty.Changed.AddClassHandler<IfControl>(ConditionPropertyChanged);
        FalseProperty.Changed.AddClassHandler<IfControl>(ConditionPropertyChanged);
    }

    private static void ConditionPropertyChanged(
        IfControl control,
        AvaloniaPropertyChangedEventArgs args
    ) =>
        control.UpdateContent();

    private void UpdateContent() => Content = Condition ? True : False;
}