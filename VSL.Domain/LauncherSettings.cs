namespace VSL.Domain;

public sealed class LauncherSettings
{
    public string DataDirectory { get; set; } = WorkspaceLayout.DefaultDataRoot;

    public string SaveDirectory { get; set; } = WorkspaceLayout.DefaultSavesRoot;

    public bool AutoStartWithWindows { get; set; }

    public string Vs2QQOneBotWsUrl { get; set; } = "ws://127.0.0.1:3001/";

    public string Vs2QQAccessToken { get; set; } = string.Empty;

    public int Vs2QQReconnectIntervalSec { get; set; } = 5;

    public string Vs2QQDatabasePath { get; set; } = string.Empty;

    public double Vs2QQPollIntervalSec { get; set; } = 1.0;

    public string Vs2QQDefaultEncoding { get; set; } = "utf-8";

    public string Vs2QQFallbackEncoding { get; set; } = "gbk";

    public List<long> Vs2QQSuperUsers { get; set; } = [];
}
