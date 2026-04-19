namespace VSL.Domain;

public sealed class ServerProfile
{
    public required string Id { get; init; }

    public required string Name { get; set; }

    public required string Version { get; set; }

    public required string DataPath { get; set; }

    public required string ActiveSaveFile { get; set; }

    public string Language { get; set; } = "zh-CN";

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
