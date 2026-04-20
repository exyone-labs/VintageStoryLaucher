using System.Diagnostics;
using VSL.Application;
using VSL.Domain;
using VSL.Infrastructure.Storage;

namespace VSL.Infrastructure.Services;

public sealed class ProfileService(IPackageService packageService) : IProfileService
{
    public async Task<IReadOnlyList<ServerProfile>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        WorkspaceLayout.EnsureWorkspaceExists();
        var index = await JsonStorage.ReadOrDefaultAsync(WorkspaceLayout.ProfilesPath, new ProfileIndex(), cancellationToken);
        return index.Profiles
            .OrderByDescending(static p => p.LastUpdatedUtc)
            .ToList();
    }

    public async Task<OperationResult<ServerProfile>> CreateProfileAsync(
        string profileName,
        string version,
        CancellationToken cancellationToken = default)
    {
        try
        {
            WorkspaceLayout.EnsureWorkspaceExists();
            if (string.IsNullOrWhiteSpace(profileName))
            {
                return OperationResult<ServerProfile>.Failed("档案名称不能为空。");
            }

            if (!packageService.IsInstalled(version))
            {
                return OperationResult<ServerProfile>.Failed($"版本 {version} 尚未安装。");
            }

            var profileId = Guid.NewGuid().ToString("N");
            var dataPath = WorkspaceLayout.GetProfileDataPath(profileId);
            var saveDirectory = WorkspaceLayout.GetProfileSavesPath(profileId);
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(saveDirectory);

            var defaultSave = WorkspaceLayout.GetProfileDefaultSaveFile(profileId);
            var profile = new ServerProfile
            {
                Id = profileId,
                Name = profileName.Trim(),
                Version = version,
                DataPath = dataPath,
                ActiveSaveFile = defaultSave,
                SaveDirectory = saveDirectory,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                LastUpdatedUtc = DateTimeOffset.UtcNow
            };

            var genConfigResult = await GenerateDefaultConfigAsync(profile, cancellationToken);
            if (!genConfigResult.IsSuccess)
            {
                return OperationResult<ServerProfile>.Failed(genConfigResult.Message ?? "生成默认配置失败。", genConfigResult.Exception);
            }

            await EnsureDefaultSaveLocationAsync(profile, cancellationToken);

            var index = await JsonStorage.ReadOrDefaultAsync(WorkspaceLayout.ProfilesPath, new ProfileIndex(), cancellationToken);
            index.Profiles.Add(profile);
            await JsonStorage.WriteAsync(WorkspaceLayout.ProfilesPath, index, cancellationToken);

            return OperationResult<ServerProfile>.Success(profile);
        }
        catch (Exception ex)
        {
            return OperationResult<ServerProfile>.Failed("创建档案失败。", ex);
        }
    }

    public async Task<OperationResult> UpdateProfileAsync(ServerProfile profile, CancellationToken cancellationToken = default)
    {
        try
        {
            var index = await JsonStorage.ReadOrDefaultAsync(WorkspaceLayout.ProfilesPath, new ProfileIndex(), cancellationToken);
            var current = index.Profiles.FirstOrDefault(x => x.Id == profile.Id);
            if (current is null)
            {
                return OperationResult.Failed("档案不存在。");
            }

            current.Name = profile.Name;
            current.Version = profile.Version;
            current.DataPath = profile.DataPath;
            current.ActiveSaveFile = profile.ActiveSaveFile;
            current.SaveDirectory = profile.SaveDirectory;
            current.Language = profile.Language;
            current.LastUpdatedUtc = DateTimeOffset.UtcNow;

            await JsonStorage.WriteAsync(WorkspaceLayout.ProfilesPath, index, cancellationToken);
            return OperationResult.Success("已更新档案。");
        }
        catch (Exception ex)
        {
            return OperationResult.Failed("更新档案失败。", ex);
        }
    }

    public async Task<OperationResult> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var index = await JsonStorage.ReadOrDefaultAsync(WorkspaceLayout.ProfilesPath, new ProfileIndex(), cancellationToken);
            var current = index.Profiles.FirstOrDefault(x => x.Id == profileId);
            if (current is null)
            {
                return OperationResult.Failed("档案不存在。");
            }

            index.Profiles.Remove(current);
            await JsonStorage.WriteAsync(WorkspaceLayout.ProfilesPath, index, cancellationToken);

            var cleanupTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                WorkspaceLayout.GetProfileRoot(profileId), // legacy workspace layout root
                current.DataPath
            };

            if (!string.IsNullOrWhiteSpace(current.SaveDirectory))
            {
                cleanupTargets.Add(current.SaveDirectory);
            }

            var activeSaveDirectory = Path.GetDirectoryName(current.ActiveSaveFile);
            if (!string.IsNullOrWhiteSpace(activeSaveDirectory))
            {
                cleanupTargets.Add(activeSaveDirectory);
            }

            foreach (var target in cleanupTargets)
            {
                TryDeleteProfileScopedDirectory(target, profileId);
            }

            return OperationResult.Success("已删除档案。");
        }
        catch (Exception ex)
        {
            return OperationResult.Failed("删除档案失败。", ex);
        }
    }

    public async Task<ServerProfile?> GetProfileByIdAsync(string profileId, CancellationToken cancellationToken = default)
    {
        var index = await JsonStorage.ReadOrDefaultAsync(WorkspaceLayout.ProfilesPath, new ProfileIndex(), cancellationToken);
        return index.Profiles.FirstOrDefault(x => x.Id == profileId);
    }

    private async Task<OperationResult> GenerateDefaultConfigAsync(ServerProfile profile, CancellationToken cancellationToken)
    {
        var installPath = packageService.GetInstallPath(profile.Version);
        var serverExe = Path.Combine(installPath, "VintagestoryServer.exe");
        if (!File.Exists(serverExe))
        {
            return OperationResult.Failed("未找到 VintagestoryServer.exe。请先安装对应版本。");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = serverExe,
                WorkingDirectory = installPath,
                Arguments = $"--genconfig --dataPath \"{profile.DataPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            }
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            var err = await process.StandardError.ReadToEndAsync(cancellationToken);
            return OperationResult.Failed($"生成 serverconfig 失败，退出码 {process.ExitCode}。{err}");
        }

        var serverConfigPath = WorkspaceLayout.GetServerConfigPath(profile.DataPath);
        return File.Exists(serverConfigPath)
            ? OperationResult.Success()
            : OperationResult.Failed("服务器未生成 serverconfig.json。");
    }

    private static async Task EnsureDefaultSaveLocationAsync(ServerProfile profile, CancellationToken cancellationToken)
    {
        var configPath = WorkspaceLayout.GetServerConfigPath(profile.DataPath);
        if (!File.Exists(configPath))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(configPath, cancellationToken);
        if (!json.Contains("\"SaveFileLocation\"", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(profile.ActiveSaveFile)!);
    }

    private static void TryDeleteProfileScopedDirectory(string path, string profileId)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch
        {
            return;
        }

        if (!Directory.Exists(fullPath))
        {
            return;
        }

        if (!IsProfileScopedPath(fullPath, profileId))
        {
            return;
        }

        Directory.Delete(fullPath, recursive: true);
    }

    private static bool IsProfileScopedPath(string path, string profileId)
    {
        var normalized = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var segments = normalized.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(x => string.Equals(x, profileId, StringComparison.OrdinalIgnoreCase));
    }
}
