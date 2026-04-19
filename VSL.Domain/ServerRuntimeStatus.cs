namespace VSL.Domain;

public sealed class ServerRuntimeStatus
{
    public bool IsRunning { get; init; }

    public int? ProcessId { get; init; }

    public DateTimeOffset? StartedAtUtc { get; init; }

    public string? ProfileId { get; init; }
}
