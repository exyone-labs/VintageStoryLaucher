using VSL.Domain;

namespace VSL.Application;

public interface IModService
{
    Task<IReadOnlyList<ModEntry>> GetModsAsync(ServerProfile profile, CancellationToken cancellationToken = default);

    Task<OperationResult<ModEntry>> ImportModZipAsync(ServerProfile profile, string zipPath, CancellationToken cancellationToken = default);

    Task<OperationResult> SetModEnabledAsync(ServerProfile profile, string modId, string version, bool enabled, CancellationToken cancellationToken = default);
}
