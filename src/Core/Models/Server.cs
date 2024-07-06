namespace Core.Models;

public class Server
{
    // Server ip and port bindings
    public string Url { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 4567;

    // Socks5 proxy
    public bool SocksProxyEnabled { get; set; }
    public int SocksProxyVersion { get; set; } = 5;
    public string SocksProxyHost { get; set; } = "";
    public string SocksProxyPort { get; set; } = "";
    public string SocksProxyUsername { get; set; } = "";
    public string SocksProxyPassword { get; set; } = "";

    // WebUI
    public bool WebUiEnabled { get; set; } = true;
    public string WebUiFlavor { get; set; } = "WebUI";
    public bool InitialOpenInBrowserEnabled { get; set; }
    public string WebUiInterface { get; set; } = "browser";
    public string ElectronPath { get; set; } = "";
    public string WebUiChannel { get; set; } = "stable";
    public int WebUiUpdateCheckInterval { get; set; } = 23;

    // Downloader
    public bool DownloadAsCbz { get; set; } = true;
    public string DownloadsPath { get; set; } = "";
    public bool AutoDownloadNewChapters { get; set; }
    public bool ExcludeEntryWithUnreadChapters { get; set; } = true;
    public int AutoDownloadNewChaptersLimit { get; set; }
    public bool AutoDownloadIgnoreReUploads { get; set; }

    // Extension Repos
    public string[] ExtensionRepos { get; set; } = [];

    // Requests
    public int MaxSourcesInParallel { get; set; } = 6;

    // Updater
    public bool ExcludeUnreadChapters { get; set; } = true;
    public bool ExcludeNotStarted { get; set; } = true;
    public bool ExcludeCompleted { get; set; } = true;
    public int GlobalUpdateInterval { get; set; } = 12;
    public bool UpdateMangas { get; set; }

    // Authentication
    public bool BasicAuthEnabled { get; set; }
    public string BasicAuthUsername { get; set; } = "";
    public string BasicAuthPassword { get; set; } = "";

    // Misc
    public bool DebugLogsEnabled { get; set; }
    public bool GqlDebugLogsEnabled { get; set; }
    public bool SystemTrayEnabled { get; set; }

    // Backup
    public string BackupPath { get; set; } = "";
    public string BackupTime { get; set; } = "00:00";
    public int BackupInterval { get; set; } = 1;
    public int BackupTtl { get; set; } = 14;

    // Local Source
    public string LocalSourcePath { get; set; } = "";

    // Cloudflare Bypass
    public bool FlareSolverrEnabled { get; set; } = true;
    public string FlareSolverrUrl { get; set; } = "http://flaresolverr:8191";
    public int FlareSolverrTimeout { get; set; } = 120;
    public string FlareSolverrSessionName { get; set; } = "suwayomi";
    public int FlareSolverrSessionTtl { get; set; } = 15;
}
