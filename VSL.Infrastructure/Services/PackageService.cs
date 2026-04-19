using System.IO.Compression;
using System.Security.Cryptography;
using VSL.Application;
using VSL.Domain;

namespace VSL.Infrastructure.Services;

public sealed class PackageService(HttpClient httpClient, IVersionCatalogService versionCatalogService) : IPackageService
{
    public bool IsInstalled(string version)
    {
        return Directory.Exists(WorkspaceLayout.GetServerInstallPath(version));
    }

    public string GetInstallPath(string version)
    {
        return WorkspaceLayout.GetServerInstallPath(version);
    }

    public async Task<OperationResult<string>> InstallReleaseAsync(
        ServerRelease release,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            WorkspaceLayout.EnsureWorkspaceExists();
            var installPath = WorkspaceLayout.GetServerInstallPath(release.Version);
            if (Directory.Exists(installPath))
            {
                return OperationResult<string>.Success(installPath, "版本已安装");
            }

            var tempRoot = Path.Combine(WorkspaceLayout.TempPath, $"install-{release.Version}-{Guid.NewGuid():N}");
            var tempExtractRoot = Path.Combine(tempRoot, "extract");
            Directory.CreateDirectory(tempExtractRoot);

            string archivePath;
            if (release.Source == ReleaseSource.LocalImport)
            {
                archivePath = release.Url;
            }
            else
            {
                archivePath = Path.Combine(tempRoot, release.FileName);
                await DownloadFileAsync(release.Url, archivePath, progress, cancellationToken);
            }

            if (!File.Exists(archivePath))
            {
                return OperationResult<string>.Failed("未找到版本压缩包文件。");
            }

            if (!string.IsNullOrWhiteSpace(release.Md5))
            {
                var actualMd5 = await ComputeMd5Async(archivePath, cancellationToken);
                if (!actualMd5.Equals(release.Md5, StringComparison.OrdinalIgnoreCase))
                {
                    return OperationResult<string>.Failed($"MD5 校验失败：期望 {release.Md5}，实际 {actualMd5}");
                }
            }

            ZipFile.ExtractToDirectory(archivePath, tempExtractRoot, overwriteFiles: true);
            Directory.Move(tempExtractRoot, installPath);

            if (release.Source == ReleaseSource.LocalImport)
            {
                await versionCatalogService.SaveLocalReleaseAsync(new LocalReleaseRecord
                {
                    Version = release.Version,
                    FileName = release.FileName,
                    ArchivePath = release.Url,
                    InstalledPath = installPath,
                    Md5 = release.Md5,
                    FileSizeBytes = release.FileSizeBytes
                }, cancellationToken);
            }

            return OperationResult<string>.Success(installPath, "安装完成");
        }
        catch (Exception ex)
        {
            return OperationResult<string>.Failed("安装版本失败。", ex);
        }
    }

    public async Task<OperationResult<ServerRelease>> ImportLocalZipAsync(
        string zipPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            WorkspaceLayout.EnsureWorkspaceExists();

            if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
            {
                return OperationResult<ServerRelease>.Failed("本地 ZIP 文件不存在。");
            }

            if (!Path.GetExtension(zipPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<ServerRelease>.Failed("仅支持导入 .zip 服务端包。");
            }

            var fileName = Path.GetFileName(zipPath);
            var version = WorkspaceLayout.TryExtractVersionFromServerFileName(fileName);
            var localPackagesRoot = Path.Combine(WorkspaceLayout.PackagesPath, "local");
            Directory.CreateDirectory(localPackagesRoot);

            var destinationPath = Path.Combine(localPackagesRoot, fileName);
            File.Copy(zipPath, destinationPath, overwrite: true);
            progress?.Report(0.65d);

            var md5 = await ComputeMd5Async(destinationPath, cancellationToken);
            var fileInfo = new FileInfo(destinationPath);
            progress?.Report(0.90d);

            var localRecord = new LocalReleaseRecord
            {
                Version = version,
                FileName = fileName,
                ArchivePath = destinationPath,
                Md5 = md5,
                FileSizeBytes = fileInfo.Length
            };

            await versionCatalogService.SaveLocalReleaseAsync(localRecord, cancellationToken);
            progress?.Report(1.0d);

            return OperationResult<ServerRelease>.Success(new ServerRelease
            {
                Version = version,
                Channel = ReleaseChannel.Local,
                Source = ReleaseSource.LocalImport,
                FileName = fileName,
                Url = destinationPath,
                Md5 = md5,
                FileSizeBytes = fileInfo.Length,
                IsLatest = false
            });
        }
        catch (Exception ex)
        {
            return OperationResult<ServerRelease>.Failed("导入本地服务端压缩包失败。", ex);
        }
    }

    private async Task DownloadFileAsync(string url, string targetPath, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[1024 * 128];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            total += read;
            if (contentLength.HasValue && contentLength.Value > 0)
            {
                progress?.Report((double)total / contentLength.Value);
            }
        }
    }

    private static async Task<string> ComputeMd5Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var md5 = MD5.Create();
        var hash = await md5.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
