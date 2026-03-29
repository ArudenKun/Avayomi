namespace Avayomi.Core;

public class Subtitle
{
    public string Url { get; set; } = null!;

    public string Language { get; set; } = null!;

    public SubtitleType Type { get; set; } = SubtitleType.Vtt;

    public Dictionary<string, string> Headers { get; set; } = [];

    public Subtitle() { }

    public Subtitle(string url, string language, SubtitleType type = SubtitleType.Vtt)
    {
        Url = url;
        Language = language;
        Type = type;
    }

    public Subtitle(
        string url,
        string language,
        Dictionary<string, string> headers,
        SubtitleType type = SubtitleType.Vtt
    )
    {
        Url = url;
        Language = language;
        Headers = headers;
        Type = type;
    }

    /// <inheritdoc />
    public override string ToString() => Language;
}
