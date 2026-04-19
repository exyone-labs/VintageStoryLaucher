namespace VSL.Domain;

public sealed class WorldRuleValue
{
    public required WorldRuleDefinition Definition { get; init; }

    public string? Value { get; set; }
}
