namespace Avayomi.Core;

public class Provider
{
    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Language { get; set; } = null!;

    public ProviderType Type { get; set; }

    public override string ToString() => Name;
}
