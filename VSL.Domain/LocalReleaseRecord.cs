namespace VSL.Domain;

public sealed class LocalReleaseRecord
{
    public required string Version { get; init; }

    public required string FileName { get; init; }

    public required string ArchivePath { get; init; }

    public string? InstalledPath { get; set; }

    public string Md5 { get; init; } = string.Empty;

    public long? FileSizeBytes { get; init; }

    public DateTimeOffset AddedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
