using System.IO.Compression;
using System.Text.Json.Nodes;
using VSL.Application;
using VSL.Domain;

namespace VSL.Infrastructure.Services;

public sealed class ModService(IServerConfigService serverConfigService) : IModService
{
    public async Task<IReadOnlyList<ModEntry>> GetModsAsync(ServerProfile profile, CancellationToken cancellationToken = default)
    {
        var modsPath = WorkspaceLayout.GetModsPath(profile.DataPath);
        Directory.CreateDirectory(modsPath);

        var disabledSet = await LoadDisabledModSetAsync(profile, cancellationToken);
        var modEntries = new List<ModEntry>();

        var files = Directory.EnumerateFiles(modsPath, "*.zip", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            modEntries.Add(ReadModFromZip(file, disabledSet));
        }

        var directories = Directory.EnumerateDirectories(modsPath, "*", SearchOption.TopDirectoryOnly);
        foreach (var directory in directories)
        {
            modEntries.Add(ReadModFromDirectory(directory, disabledSet));
        }

        var enabledModIds = modEntries
            .Where(static m => !m.IsDisabled)
            .Select(static m => m.ModId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var normalized = modEntries.Select(mod =>
        {
            var issues = new List<string>(mod.DependencyIssues);
            foreach (var dependency in mod.Dependencies)
            {
                if (!enabledModIds.Contains(dependency.ModId))
                {
                    issues.Add($"缺少依赖: {dependency.ModId}{(string.IsNullOrWhiteSpace(dependency.Version) ? string.Empty : $"@{dependency.Version}")}");
                }
            }

            var status = mod.Status;
            if (issues.Count > 0)
            {
                status = "MissingDependency";
            }

            return new ModEntry
            {
                ModId = mod.ModId,
                Version = mod.Version,
                FilePath = mod.FilePath,
                Status = status,
                IsDisabled = mod.IsDisabled,
                Dependencies = mod.Dependencies,
                DependencyIssues = issues
            };
        }).OrderBy(static x => x.ModId, StringComparer.OrdinalIgnoreCase).ToList();

        return normalized;
    }

    public async Task<OperationResult<ModEntry>> ImportModZipAsync(ServerProfile profile, string zipPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
            {
                return OperationResult<ModEntry>.Failed("Mod ZIP 文件不存在。");
            }

            if (!Path.GetExtension(zipPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<ModEntry>.Failed("仅支持导入 ZIP 格式 Mod。");
            }

            var modsPath = WorkspaceLayout.GetModsPath(profile.DataPath);
            Directory.CreateDirectory(modsPath);
            var fileName = WorkspaceLayout.SanitizeFileName(Path.GetFileName(zipPath));
            var destinationPath = Path.Combine(modsPath, fileName);
            File.Copy(zipPath, destinationPath, overwrite: true);

            var disabledSet = await LoadDisabledModSetAsync(profile, cancellationToken);
            return OperationResult<ModEntry>.Success(ReadModFromZip(destinationPath, disabledSet), "导入 Mod 成功。");
        }
        catch (Exception ex)
        {
            return OperationResult<ModEntry>.Failed("导入 Mod 失败。", ex);
        }
    }

    public async Task<OperationResult> SetModEnabledAsync(ServerProfile profile, string modId, string version, bool enabled, CancellationToken cancellationToken = default)
    {
        try
        {
            var configResult = await serverConfigService.LoadRawJsonAsync(profile, cancellationToken);
            if (!configResult.IsSuccess || configResult.Value is null)
            {
                return OperationResult.Failed(configResult.Message ?? "读取配置失败。", configResult.Exception);
            }

            var root = JsonNode.Parse(configResult.Value) as JsonObject;
            if (root is null)
            {
                return OperationResult.Failed("配置格式错误。");
            }

            var disabledArray = GetOrCreateDisabledModsArray(root);
            var modVersionKey = $"{modId}@{version}";
            var values = disabledArray
                .Where(static x => x is not null)
                .Select(static x => x!.GetValue<string>())
                .ToList();

            values.RemoveAll(v =>
                v.Equals(modId, StringComparison.OrdinalIgnoreCase) ||
                v.Equals(modVersionKey, StringComparison.OrdinalIgnoreCase));

            if (!enabled)
            {
                values.Add(modVersionKey);
            }

            disabledArray.Clear();
            foreach (var value in values.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                disabledArray.Add(value);
            }

            var saveResult = await serverConfigService.SaveRawJsonAsync(profile, root.ToJsonString(new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            }), cancellationToken);

            return saveResult.IsSuccess
                ? OperationResult.Success(enabled ? "Mod 已启用。" : "Mod 已禁用。")
                : saveResult;
        }
        catch (Exception ex)
        {
            return OperationResult.Failed("更新 Mod 启停状态失败。", ex);
        }
    }

    private static ModEntry ReadModFromZip(string zipPath, HashSet<string> disabledSet)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.FirstOrDefault(x => x.FullName.EndsWith("modinfo.json", StringComparison.OrdinalIgnoreCase));
            if (entry is null)
            {
                return BuildFallbackEntry(zipPath, "InvalidMetadata", disabledSet);
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return BuildEntryFromModInfo(json, zipPath, disabledSet);
        }
        catch
        {
            return BuildFallbackEntry(zipPath, "InvalidMetadata", disabledSet);
        }
    }

    private static ModEntry ReadModFromDirectory(string directoryPath, HashSet<string> disabledSet)
    {
        try
        {
            var modInfoPath = Path.Combine(directoryPath, "modinfo.json");
            if (!File.Exists(modInfoPath))
            {
                return BuildFallbackEntry(directoryPath, "InvalidMetadata", disabledSet);
            }

            var json = File.ReadAllText(modInfoPath);
            return BuildEntryFromModInfo(json, directoryPath, disabledSet);
        }
        catch
        {
            return BuildFallbackEntry(directoryPath, "InvalidMetadata", disabledSet);
        }
    }

    private static ModEntry BuildEntryFromModInfo(string modInfoJson, string filePath, HashSet<string> disabledSet)
    {
        var node = JsonNode.Parse(modInfoJson) as JsonObject;
        if (node is null)
        {
            return BuildFallbackEntry(filePath, "InvalidMetadata", disabledSet);
        }

        var modId = node["modid"]?.GetValue<string>() ?? Path.GetFileNameWithoutExtension(filePath);
        var version = node["version"]?.GetValue<string>() ?? "unknown";
        var dependencies = ReadDependencies(node["dependencies"]);
        var disabled = disabledSet.Contains(modId) || disabledSet.Contains($"{modId}@{version}");

        return new ModEntry
        {
            ModId = modId,
            Version = version,
            FilePath = filePath,
            Status = "OK",
            IsDisabled = disabled,
            Dependencies = dependencies,
            DependencyIssues = []
        };
    }

    private static ModEntry BuildFallbackEntry(string filePath, string status, HashSet<string> disabledSet)
    {
        var fallbackId = Path.GetFileNameWithoutExtension(filePath);
        return new ModEntry
        {
            ModId = fallbackId,
            Version = "unknown",
            FilePath = filePath,
            Status = status,
            IsDisabled = disabledSet.Contains(fallbackId),
            Dependencies = [],
            DependencyIssues = []
        };
    }

    private static IReadOnlyList<ModDependency> ReadDependencies(JsonNode? dependenciesNode)
    {
        if (dependenciesNode is not JsonArray dependenciesArray)
        {
            return [];
        }

        var dependencies = new List<ModDependency>();
        foreach (var item in dependenciesArray)
        {
            if (item is not JsonObject dependencyObject)
            {
                continue;
            }

            var modId = dependencyObject["modid"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(modId))
            {
                continue;
            }

            dependencies.Add(new ModDependency
            {
                ModId = modId,
                Version = dependencyObject["version"]?.GetValue<string>()
            });
        }

        return dependencies;
    }

    private async Task<HashSet<string>> LoadDisabledModSetAsync(ServerProfile profile, CancellationToken cancellationToken)
    {
        var result = await serverConfigService.LoadRawJsonAsync(profile, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return [];
        }

        var root = JsonNode.Parse(result.Value) as JsonObject;
        if (root is null)
        {
            return [];
        }

        var array = root["WorldConfig"]?["DisabledMods"] as JsonArray;
        if (array is null)
        {
            return [];
        }

        return array
            .Where(static x => x is not null)
            .Select(static x => x!.GetValue<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static JsonArray GetOrCreateDisabledModsArray(JsonObject root)
    {
        if (root["WorldConfig"] is not JsonObject worldConfig)
        {
            worldConfig = new JsonObject();
            root["WorldConfig"] = worldConfig;
        }

        if (worldConfig["DisabledMods"] is JsonArray disabledMods)
        {
            return disabledMods;
        }

        disabledMods = new JsonArray();
        worldConfig["DisabledMods"] = disabledMods;
        return disabledMods;
    }
}
