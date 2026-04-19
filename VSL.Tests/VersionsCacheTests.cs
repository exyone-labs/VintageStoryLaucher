using VSL.Domain;
using VSL.Infrastructure.Services;
using VSL.Tests.TestSupport;

namespace VSL.Tests;

public sealed class VersionsCacheTests
{
    [Fact]
    public async Task SaveLocalReleaseAsync_PersistsAndCanReadBack()
    {
        WorkspaceLayout.EnsureWorkspaceExists();

        var cachePath = WorkspaceLayout.VersionsCachePath;
        var backupPath = cachePath + ".bak." + Guid.NewGuid().ToString("N");
        if (File.Exists(cachePath))
        {
            File.Copy(cachePath, backupPath, overwrite: true);
        }

        try
        {
            var service = new VersionCatalogService(FakeHttpMessageHandler.CreateJsonClient("{}"));
            var version = "local-test-" + Guid.NewGuid().ToString("N");

            await service.SaveLocalReleaseAsync(new LocalReleaseRecord
            {
                Version = version,
                FileName = "local.zip",
                ArchivePath = @"E:\vintagestory\VSL\workspace\packages\local\local.zip",
                Md5 = "deadbeef",
                FileSizeBytes = 12345
            });

            var localReleases = await service.GetLocalReleasesAsync();

            Assert.Contains(localReleases, r => r.Version == version && r.Source == ReleaseSource.LocalImport);
        }
        finally
        {
            if (File.Exists(backupPath))
            {
                File.Copy(backupPath, cachePath, overwrite: true);
                File.Delete(backupPath);
            }
            else if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }
        }
    }
}
