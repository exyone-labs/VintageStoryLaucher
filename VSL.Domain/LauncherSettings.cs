namespace VSL.Domain;

public sealed class LauncherSettings
{
    public string DataDirectory { get; set; } = WorkspaceLayout.DefaultDataRoot;

    public string SaveDirectory { get; set; } = WorkspaceLayout.DefaultSavesRoot;

    public bool AutoStartWithWindows { get; set; }
}
