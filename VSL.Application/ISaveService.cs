using VSL.Domain;

namespace VSL.Application;

public interface ISaveService
{
    Task<IReadOnlyList<SaveFileEntry>> GetSavesAsync(ServerProfile profile, CancellationToken cancellationToken = default);

    Task<OperationResult<string>> CreateSaveAsync(ServerProfile profile, string saveName, CancellationToken cancellationToken = default);

    Task<OperationResult> SetActiveSaveAsync(ServerProfile profile, string saveFilePath, CancellationToken cancellationToken = default);

    Task<OperationResult<string>> BackupActiveSaveAsync(ServerProfile profile, CancellationToken cancellationToken = default);
}
