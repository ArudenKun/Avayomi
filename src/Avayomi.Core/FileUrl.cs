namespace Avayomi.Core;

public class FileUrl
{
    public string Url { get; set; } = string.Empty;

    public Dictionary<string, string> Headers { get; set; } = [];

    public FileUrl() { }

    public FileUrl(string url)
    {
        Url = url;
    }

    public FileUrl(string url, Dictionary<string, string> headers)
    {
        Url = url;
        Headers = headers;
    }
}
