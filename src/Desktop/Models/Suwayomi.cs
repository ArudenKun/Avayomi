namespace Desktop.Models;

public class Suwayomi
{
    public bool IsLocalHost { get; set; } = true;
    public Server Server { get; set; } = new();
}
