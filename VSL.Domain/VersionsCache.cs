namespace VSL.Domain;

public sealed class VersionsCache
{
    public DateTimeOffset LastRefreshedUtc { get; set; } = DateTimeOffset.MinValue;

    public List<LocalReleaseRecord> LocalReleases { get; init; } = [];
}
