using VSL.Domain;

namespace VSL.Application;

public interface IProfileService
{
    Task<IReadOnlyList<ServerProfile>> GetProfilesAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<ServerProfile>> CreateProfileAsync(
        string profileName,
        string version,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateProfileAsync(ServerProfile profile, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default);

    Task<ServerProfile?> GetProfileByIdAsync(string profileId, CancellationToken cancellationToken = default);
}
