namespace Avayomi.Core.Videos;

/// <summary>
/// A simple class containing name and link of the embed which shows the video present on the site.
/// </summary>
public class VideoServer
{
    public string Name { get; set; } = null!;

    public FileUrl Embed { get; set; } = null!;

    /// <summary>
    /// Initializes an instance of <see cref="VideoServer"/>.
    /// </summary>
    public VideoServer() { }

    /// <summary>
    /// Initializes an instance of <see cref="VideoServer"/>.
    /// </summary>
    public VideoServer(string url)
    {
        Name = "Default Server";
        Embed = new FileUrl(url);
    }

    /// <summary>
    /// Initializes an instance of <see cref="VideoServer"/>.
    /// </summary>
    public VideoServer(string name, FileUrl embed)
    {
        Name = name;
        Embed = embed;
    }

    /// <summary>
    /// Initializes an instance of <see cref="VideoServer"/>.
    /// </summary>
    public VideoServer(string name, string url)
    {
        Name = name;
        Embed = new(url);
    }

    /// <inheritdoc />
    public override string ToString() => Name;
}
