namespace VSL.Domain;

public sealed class ModDependency
{
    public required string ModId { get; init; }

    public string? Version { get; init; }
}
