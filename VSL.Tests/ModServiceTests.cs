using System.Text.Json.Nodes;
using VSL.Domain;
using VSL.Infrastructure.Services;
using VSL.Tests.TestSupport;

namespace VSL.Tests;

public sealed class ModServiceTests
{
    [Fact]
    public async Task SetModEnabledAsync_UpdatesDisabledModsList()
    {
        var dataPath = TestWorkspace.CreateProfileDataPath();
        var profile = new ServerProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "mod-test",
            Version = "1.21.7",
            DataPath = dataPath,
            ActiveSaveFile = Path.Combine(dataPath, "Saves", "default.vcdbs")
        };

        try
        {
            var configPath = WorkspaceLayout.GetServerConfigPath(dataPath);
            await File.WriteAllTextAsync(configPath,
                """
                {
                  "WorldConfig": {
                    "DisabledMods": ["already@1.0.0"]
                  }
                }
                """);

            var configService = new ServerConfigService();
            var modService = new ModService(configService);

            var disableResult = await modService.SetModEnabledAsync(profile, "samplemod", "2.0.0", enabled: false);
            Assert.True(disableResult.IsSuccess, disableResult.Message);

            var nodeAfterDisable = await ReadJsonObjectWithRetryAsync(configPath);
            var disabledArray = nodeAfterDisable["WorldConfig"]!["DisabledMods"]!.AsArray().Select(x => x!.GetValue<string>()).ToList();
            Assert.Contains("samplemod@2.0.0", disabledArray);

            var enableResult = await modService.SetModEnabledAsync(profile, "samplemod", "2.0.0", enabled: true);
            Assert.True(enableResult.IsSuccess, enableResult.Message);

            var nodeAfterEnable = await ReadJsonObjectWithRetryAsync(configPath);
            var disabledArrayAfterEnable = nodeAfterEnable["WorldConfig"]!["DisabledMods"]!.AsArray().Select(x => x!.GetValue<string>()).ToList();
            Assert.DoesNotContain("samplemod@2.0.0", disabledArrayAfterEnable);
            Assert.Contains("already@1.0.0", disabledArrayAfterEnable);
        }
        finally
        {
            TestWorkspace.CleanupProfileDataPath(dataPath);
        }
    }

    private static async Task<JsonObject> ReadJsonObjectWithRetryAsync(string path)
    {
        const int retries = 5;
        for (var i = 0; i < retries; i++)
        {
            try
            {
                return JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
            }
            catch (IOException) when (i < retries - 1)
            {
                await Task.Delay(50);
            }
        }

        throw new IOException($"Unable to read json after retries: {path}");
    }
}
