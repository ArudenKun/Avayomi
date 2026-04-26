using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avayomi.Media;

public partial class Media : ObservableObject
{
    [ObservableProperty]
    public partial string? FileName { get; set; }

    [ObservableProperty]
    public partial string? VideoUrl { get; set; }

    [ObservableProperty]
    public partial (string, string)? OnlineUrls { get; set; }

    [ObservableProperty]
    public partial IDictionary<string, string> Headers { get; set; } =
        new Dictionary<string, string>();
}
