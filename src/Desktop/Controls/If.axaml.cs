using Avalonia;
using Avalonia.Controls;

namespace Desktop.Controls;

public partial class If : UserControl
{
    public If()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<bool> ConditionProperty = AvaloniaProperty.Register<
        If,
        bool
    >(nameof(Condition));

    public static readonly StyledProperty<Control> TrueProperty = AvaloniaProperty.Register<
        If,
        Control
    >(nameof(True));

    public static readonly StyledProperty<Control> FalseProperty = AvaloniaProperty.Register<
        If,
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

    static If()
    {
        ConditionProperty.Changed.AddClassHandler<If>(ConditionPropertyChanged);
        TrueProperty.Changed.AddClassHandler<If>(ConditionPropertyChanged);
        FalseProperty.Changed.AddClassHandler<If>(ConditionPropertyChanged);
    }

    private static void ConditionPropertyChanged(
        If control,
        AvaloniaPropertyChangedEventArgs args
    ) =>
        control.UpdateContent();

    private void UpdateContent() => Content = Condition ? True : False;
}