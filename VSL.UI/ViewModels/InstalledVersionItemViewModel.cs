namespace VSL.UI.ViewModels;

public sealed class InstalledVersionItemViewModel
{
    public required string Version { get; init; }

    public required string InstallPath { get; init; }

    public int ProfileCount { get; init; }

    public bool IsUsedByProfile => ProfileCount > 0;

    public string UsageText => IsUsedByProfile
        ? $"被 {ProfileCount} 个档案使用"
        : "未被档案使用";
}
