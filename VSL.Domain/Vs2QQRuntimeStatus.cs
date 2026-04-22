namespace VSL.Domain;

public sealed class Vs2QQRuntimeStatus
{
    public bool IsRunning { get; init; }

    public int? ProcessId { get; init; }

    public DateTimeOffset? StartedAtUtc { get; init; }

    public string? OneBotWsUrl { get; init; }
}
