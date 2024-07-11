using Avalonia.Controls;

namespace Desktop.Controls.SpaceGrid;

public class SpacingColumnDefinition(double width)
    : ColumnDefinition(width, GridUnitType.Pixel),
        ISpacingDefinition
{
    public double Spacing
    {
        get => Width.Value;
        set => Width = new GridLength(value, GridUnitType.Pixel);
    }
}
