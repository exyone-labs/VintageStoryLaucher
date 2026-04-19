using System.IO.Compression;
using System.Security.Cryptography;
using VSL.Application;
using VSL.Domain;
using VSL.Infrastructure.Services;
using VSL.Tests.TestSupport;

namespace VSL.Tests;

public sealed class PackageServiceTests
{
    [Fact]
    public async Task InstallReleaseAsync_ExtractsLocalZipAfterMd5Validation()
    {
        WorkspaceLayout.EnsureWorkspaceExists();

        var zipPath = TestWorkspace.CreateTempFilePath(".zip");
        var version = $"test-{Guid.NewGuid():N}";
        var installPath = WorkspaceLayout.GetServerInstallPath(version);

        try
        {
            await using (var file = File.Create(zipPath))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("VintagestoryServer.exe");
                await using var writer = new StreamWriter(entry.Open());
                await writer.WriteAsync("fake-server");
            }

            var md5 = await ComputeMd5Async(zipPath);

            var service = new PackageService(new HttpClient(), new StubVersionCatalogService());
            var release = new ServerRelease
            {
                Version = version,
                Channel = ReleaseChannel.Local,
                Source = ReleaseSource.LocalImport,
                FileName = Path.GetFileName(zipPath),
                Url = zipPath,
                Md5 = md5,
                IsLatest = false
            };

            var result = await service.InstallReleaseAsync(release);

            Assert.True(result.IsSuccess, result.Message);
            Assert.True(Directory.Exists(installPath));
            Assert.True(File.Exists(Path.Combine(installPath, "VintagestoryServer.exe")));
        }
        finally
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            if (Directory.Exists(installPath))
            {
                Directory.Delete(installPath, recursive: true);
            }
        }
    }

    private static async Task<string> ComputeMd5Async(string filePath)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var md5 = MD5.Create();
        var hash = await md5.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class StubVersionCatalogService : IVersionCatalogService
    {
        public Task<IReadOnlyList<ServerRelease>> GetOfficialReleasesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ServerRelease>>([]);

        public Task<IReadOnlyList<ServerRelease>> GetLocalReleasesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ServerRelease>>([]);

        public Task SaveLocalReleaseAsync(LocalReleaseRecord release, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
