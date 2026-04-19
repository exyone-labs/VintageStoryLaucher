namespace VSL.Domain;

public sealed class SaveFileEntry
{
    public required string FullPath { get; init; }

    public required string FileName { get; init; }

    public DateTimeOffset LastWriteTimeUtc { get; init; }
}
