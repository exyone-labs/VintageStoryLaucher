using System.Text.Json.Nodes;
using VSL.Domain;
using VSL.Infrastructure.Services;
using VSL.Tests.TestSupport;

namespace VSL.Tests;

public sealed class ServerConfigServiceTests
{
    [Fact]
    public async Task SaveCommonSettingsAsync_PreservesUnknownFields()
    {
        var dataPath = TestWorkspace.CreateProfileDataPath();
        var profile = new ServerProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "config-test",
            Version = "1.21.7",
            DataPath = dataPath,
            ActiveSaveFile = Path.Combine(dataPath, "Saves", "default.vcdbs")
        };

        try
        {
            var configPath = WorkspaceLayout.GetServerConfigPath(dataPath);
            Directory.CreateDirectory(dataPath);
            await File.WriteAllTextAsync(configPath,
                """
                {
                  "ServerName": "Before",
                  "Port": 42420,
                  "RootUnknown": { "keepMe": true },
                  "WorldConfig": {
                    "Seed": "oldSeed",
                    "WorldName": "oldName",
                    "SaveFileLocation": "old.vcdbs",
                    "PlayStyle": "surviveandbuild",
                    "WorldType": "standard",
                    "MapSizeY": 256,
                    "WorldConfiguration": {
                      "allowMap": true,
                      "UnknownRule": "keep"
                    }
                  }
                }
                """);

            var service = new ServerConfigService();
            var settings = new ServerCommonSettings { ServerName = "After", Port = 42421, MaxClients = 20 };
            var world = new WorldSettings
            {
                Seed = "newSeed",
                WorldName = "newWorld",
                SaveFileLocation = profile.ActiveSaveFile,
                PlayStyle = "surviveandbuild",
                WorldType = "standard",
                WorldHeight = 320
            };

            var rules = new List<WorldRuleValue>
            {
                new()
                {
                    Definition = WorldRuleCatalog.DefaultRules.First(x => x.Key == "allowMap"),
                    Value = "false"
                }
            };

            var saveResult = await service.SaveCommonSettingsAsync(profile, settings, world, rules);
            Assert.True(saveResult.IsSuccess, saveResult.Message);

            var node = JsonNode.Parse(await File.ReadAllTextAsync(configPath))!.AsObject();
            Assert.NotNull(node["RootUnknown"]);
            Assert.True(node["RootUnknown"]!["keepMe"]!.GetValue<bool>());
            Assert.Equal("keep", node["WorldConfig"]!["WorldConfiguration"]!["UnknownRule"]!.GetValue<string>());
            Assert.Equal("After", node["ServerName"]!.GetValue<string>());
        }
        finally
        {
            TestWorkspace.CleanupProfileDataPath(dataPath);
        }
    }
}
