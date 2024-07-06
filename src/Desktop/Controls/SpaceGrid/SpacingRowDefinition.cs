using Avalonia.Controls;

namespace Desktop.Controls.SpaceGrid;

public class SpacingRowDefinition(double height) : RowDefinition(height, GridUnitType.Pixel), ISpacingDefinition
{
    public double Spacing
    {
        get => Height.Value;
        set => Height = new GridLength(value, GridUnitType.Pixel);
    }
}