namespace Avayomi.Core;

public static class HttpHelper
{
    public const string ProviderHttpClientName = "Provider:HttpClient";

    /// <summary>
    /// Generates a random User-Agent from the Chrome browser.
    /// </summary>
    /// <returns>Random User-Agent from Chrome browser.</returns>
    public static string ChromeUserAgent()
    {
        var random = new Random();
        var major = random.Next(62, 70);
        var build = random.Next(2100, 3538);
        var branchBuild = random.Next(170);

        return $"Mozilla/5.0 ({RandomWindowsVersion()}) AppleWebKit/537.36 (KHTML, like Gecko) "
            + $"Chrome/{major}.0.{build}.{branchBuild} Safari/537.36";
    }

    private static string RandomWindowsVersion()
    {
        var random = new Random();

        var windowsVersion = "Windows NT ";
        var val = random.Next(99) + 1;

        // Windows 10 = 45% popularity
        if (val >= 1 && val <= 45)
            windowsVersion += "10.0";
        // Windows 7 = 35% popularity
        else if (val > 45 && val <= 80)
            windowsVersion += "6.1";
        // Windows 8.1 = 15% popularity
        else if (val > 80 && val <= 95)
            windowsVersion += "6.3";
        // Windows 8 = 5% popularity
        else
            windowsVersion += "6.2";

        // Append WOW64 for X64 system
        if (random.NextDouble() <= 0.65)
            windowsVersion += random.NextDouble() <= 0.5 ? "; WOW64" : "; Win64; x64";

        return windowsVersion;
    }
}
