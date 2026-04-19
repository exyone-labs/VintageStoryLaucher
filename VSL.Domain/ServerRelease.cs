namespace VSL.Domain;

public sealed class ServerRelease
{
    public required string Version { get; init; }

    public required ReleaseChannel Channel { get; init; }

    public required ReleaseSource Source { get; init; }

    public required string FileName { get; init; }

    public required string Url { get; init; }

    public required string Md5 { get; init; }

    public long? FileSizeBytes { get; init; }

    public bool IsLatest { get; init; }

    public string? InstalledPath { get; init; }

    public string DisplayName => $"{Version} ({Channel})";
}
