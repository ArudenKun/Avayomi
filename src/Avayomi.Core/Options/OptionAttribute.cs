namespace Avayomi.Core.Options;

[AttributeUsage(AttributeTargets.Class)]
public class OptionAttribute : Attribute
{
    public OptionAttribute(string? section)
    {
        Section = section;
    }

    public string? Section { get; }
}
