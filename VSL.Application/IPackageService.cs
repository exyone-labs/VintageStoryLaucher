using VSL.Domain;

namespace VSL.Application;

public interface IPackageService
{
    bool IsInstalled(string version);

    string GetInstallPath(string version);

    Task<IReadOnlyList<string>> GetInstalledVersionsAsync(CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteInstalledVersionAsync(string version, CancellationToken cancellationToken = default);

    Task<OperationResult<string>> InstallReleaseAsync(
        ServerRelease release,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ServerRelease>> ImportLocalZipAsync(
        string zipPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
