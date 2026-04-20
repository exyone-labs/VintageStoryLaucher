using VSL.Domain;

namespace VSL.Application;

public interface ILauncherSettingsService
{
    Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task<OperationResult> SaveAsync(LauncherSettings settings, CancellationToken cancellationToken = default);
}
