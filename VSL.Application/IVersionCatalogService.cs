using VSL.Domain;

namespace VSL.Application;

public interface IVersionCatalogService
{
    Task<IReadOnlyList<ServerRelease>> GetOfficialReleasesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServerRelease>> GetLocalReleasesAsync(CancellationToken cancellationToken = default);

    Task SaveLocalReleaseAsync(LocalReleaseRecord release, CancellationToken cancellationToken = default);
}
