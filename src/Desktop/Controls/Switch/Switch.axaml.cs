using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Generator.Attributes;

namespace Desktop.Controls.Switch;

[AvaloniaProperty("CurrentCase", typeof(Case))]
[AvaloniaProperty("SwitchCases", typeof(CaseCollection), Attributes = [typeof(ContentAttribute)])]
[AvaloniaProperty("Value", typeof(object))]
[AvaloniaProperty("TargetType", typeof(Type))]
[RequiresUnreferencedCode("Calls TypeDescriptor.GetConverter(Type) which uses reflection")]
public partial class Switch : UserControl
{
    public Switch()
    {
        InitializeComponent();
        SwitchCases = new CaseCollection();
    }

    partial void OnValueChanged()
    {
        EvaluateCases();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        EvaluateCases();
    }

    private void EvaluateCases()
    {
        if (SwitchCases == null || SwitchCases.Count == 0)
        {
            // If we have no cases, then we can't match anything.
            if (CurrentCase != null)
            {
                // Only bother clearing our actual content if we had something before.
                Content = null;
                CurrentCase = null;
            }

            return;
        }

        if (CurrentCase?.Value != null && CurrentCase.Value.Equals(Value))
        {
            // If the current case we're on already matches our current value,
            // then we don't have any work to do.
            return;
        }

        Case? xdefault = null;
        Case? newcase = null;

        foreach (var xcase in SwitchCases)
        {
            if (xcase.IsDefault)
            {
                // If there are multiple default cases provided, this will override and just grab the last one, the developer will have to fix this in their XAML. We call this out in the case comments.
                xdefault = xcase;
                continue;
            }

            if (!CompareValues(Value, xcase.Value))
                continue;

            newcase = xcase;
            break;
        }

        if (newcase == null && xdefault != null)
        {
            // Inject default if we found one without matching anything
            newcase = xdefault;
        }

        // Only bother changing things around if we actually have a new case.
        if (newcase != CurrentCase)
        {
            // If we don't have any cases or default, setting these to null is what we want to be blank again.
            Content = newcase?.Content;
            CurrentCase = newcase;
        }
    }

    /// <summary>
    /// Compares two values using the TargetType.
    /// </summary>
    /// <param name="compare">Our main value in our SwitchPresenter.</param>
    /// <param name="value">The value from the case to compare to.</param>
    /// <returns>true if the two values are equal</returns>
    private bool CompareValues(object? compare, object? value)
    {
        if (compare == null || value == null)
        {
            return compare == value;
        }

        if (
            TargetType == null
            || (TargetType == compare.GetType() && TargetType == value.GetType())
        )
        {
            // Default direct object comparison or we're all the proper type
            return compare.Equals(value);
        }

        if (compare.GetType() == TargetType)
        {
            // If we have a TargetType and the first value is the right type
            // Then our 2nd value isn't, so convert to string and coerce.
            var valueBase2 = ConvertValue(TargetType, value);

            return compare.Equals(valueBase2);
        }

        // Neither of our two values matches the type so
        // we'll convert both to a String and try and coerce it to the proper type.
        var compareBase = ConvertValue(TargetType, compare);

        var valueBase = ConvertValue(TargetType, value);

        return compareBase.Equals(valueBase);
    }

    /// <summary>
    /// Helper method to convert a value from a source type to a target type.
    /// </summary>
    /// <param name="targetType">The target type</param>
    /// <param name="value">The value to convert</param>
    /// <returns>The converted value</returns>
    private static object ConvertValue(Type targetType, object value)
    {
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        switch (targetType.IsEnum)
        {
            case true when value is string str:
            {
                if (Enum.TryParse(targetType, str, out var result))
                {
                    return result;
                }

                static object ThrowExceptionForKeyNotFound()
                {
                    throw new InvalidOperationException(
                        "The requested enum value was not present in the provided type."
                    );
                }

                return ThrowExceptionForKeyNotFound();
            }
            default:
                var converter = TypeDescriptor.GetConverter(targetType);
                return converter.ConvertTo(value, targetType)!;
            // return XamlBindingHelper.ConvertValue(targetType, value);
        }
    }
}
