using VSL.Domain;
using VSL.Infrastructure.Services;
using VSL.Tests.TestSupport;

namespace VSL.Tests;

public sealed class VersionCatalogServiceTests
{
    [Fact]
    public async Task GetOfficialReleasesAsync_ParsesWindowsServerStableAndUnstable()
    {
        const string payload = """
                               {
                                 "1.21.7": {
                                   "windowsserver": {
                                     "filename": "vs_server_win-x64_1.21.7.zip",
                                     "md5": "abc123",
                                     "latest": 1,
                                     "urls": {
                                       "cdn": "https://cdn.vintagestory.at/gamefiles/stable/vs_server_win-x64_1.21.7.zip"
                                     }
                                   }
                                 },
                                 "1.22.0-rc.10": {
                                   "windowsserver": {
                                     "filename": "vs_server_win-x64_1.22.0-rc.10.zip",
                                     "md5": "def456",
                                     "urls": {
                                       "cdn": "https://cdn.vintagestory.at/gamefiles/unstable/vs_server_win-x64_1.22.0-rc.10.zip"
                                     }
                                   }
                                 },
                                 "1.21.7-extra": {
                                   "linuxserver": {}
                                 }
                               }
                               """;

        var service = new VersionCatalogService(FakeHttpMessageHandler.CreateJsonClient(payload));

        var releases = await service.GetOfficialReleasesAsync();

        Assert.Equal(2, releases.Count);
        Assert.Contains(releases, r => r.Version == "1.21.7" && r.Channel == ReleaseChannel.Stable);
        Assert.Contains(releases, r => r.Version == "1.22.0-rc.10" && r.Channel == ReleaseChannel.Unstable);
        Assert.All(releases, r => Assert.Equal(ReleaseSource.Official, r.Source));
    }
}
