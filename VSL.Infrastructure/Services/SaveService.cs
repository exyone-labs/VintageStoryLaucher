using VSL.Application;
using VSL.Domain;

namespace VSL.Infrastructure.Services;

public sealed class SaveService(IServerConfigService serverConfigService) : ISaveService
{
    public Task<IReadOnlyList<SaveFileEntry>> GetSavesAsync(ServerProfile profile, CancellationToken cancellationToken = default)
    {
        var savesPath = WorkspaceLayout.GetSavesPath(profile.DataPath);
        Directory.CreateDirectory(savesPath);

        IReadOnlyList<SaveFileEntry> result = Directory
            .EnumerateFiles(savesPath, "*.vcdbs", SearchOption.TopDirectoryOnly)
            .Select(path =>
            {
                var fileInfo = new FileInfo(path);
                return new SaveFileEntry
                {
                    FullPath = fileInfo.FullName,
                    FileName = fileInfo.Name,
                    LastWriteTimeUtc = fileInfo.LastWriteTimeUtc
                };
            })
            .OrderByDescending(static x => x.LastWriteTimeUtc)
            .ToList();

        return Task.FromResult(result);
    }

    public async Task<OperationResult<string>> CreateSaveAsync(ServerProfile profile, string saveName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(saveName))
            {
                return OperationResult<string>.Failed("存档名称不能为空。");
            }

            var savesPath = WorkspaceLayout.GetSavesPath(profile.DataPath);
            Directory.CreateDirectory(savesPath);

            var fileName = WorkspaceLayout.SanitizeFileName(saveName.Trim()) + ".vcdbs";
            var fullPath = Path.Combine(savesPath, fileName);

            if (!File.Exists(fullPath))
            {
                await using var _ = File.Create(fullPath);
            }

            var setResult = await SetActiveSaveAsync(profile, fullPath, cancellationToken);
            if (!setResult.IsSuccess)
            {
                return OperationResult<string>.Failed(setResult.Message ?? "切换新存档失败。", setResult.Exception);
            }

            return OperationResult<string>.Success(fullPath);
        }
        catch (Exception ex)
        {
            return OperationResult<string>.Failed("创建存档失败。", ex);
        }
    }

    public async Task<OperationResult> SetActiveSaveAsync(ServerProfile profile, string saveFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var worldSettingsResult = await serverConfigService.LoadWorldSettingsAsync(profile, cancellationToken);
            if (!worldSettingsResult.IsSuccess || worldSettingsResult.Value is null)
            {
                return OperationResult.Failed(worldSettingsResult.Message ?? "读取世界配置失败。", worldSettingsResult.Exception);
            }

            var serverSettingsResult = await serverConfigService.LoadServerSettingsAsync(profile, cancellationToken);
            if (!serverSettingsResult.IsSuccess || serverSettingsResult.Value is null)
            {
                return OperationResult.Failed(serverSettingsResult.Message ?? "读取服务器配置失败。", serverSettingsResult.Exception);
            }

            var rulesResult = await serverConfigService.LoadWorldRulesAsync(profile, cancellationToken);
            if (!rulesResult.IsSuccess || rulesResult.Value is null)
            {
                return OperationResult.Failed(rulesResult.Message ?? "读取世界规则失败。", rulesResult.Exception);
            }

            var worldSettings = worldSettingsResult.Value;
            worldSettings.SaveFileLocation = saveFilePath;

            var saveResult = await serverConfigService.SaveCommonSettingsAsync(
                profile,
                serverSettingsResult.Value,
                worldSettings,
                rulesResult.Value,
                cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

            profile.ActiveSaveFile = saveFilePath;
            profile.LastUpdatedUtc = DateTimeOffset.UtcNow;
            return OperationResult.Success("已切换当前存档。");
        }
        catch (Exception ex)
        {
            return OperationResult.Failed("切换存档失败。", ex);
        }
    }

    public async Task<OperationResult<string>> BackupActiveSaveAsync(ServerProfile profile, CancellationToken cancellationToken = default)
    {
        try
        {
            var worldSettings = await serverConfigService.LoadWorldSettingsAsync(profile, cancellationToken);
            if (!worldSettings.IsSuccess || worldSettings.Value is null)
            {
                return OperationResult<string>.Failed(worldSettings.Message ?? "读取世界配置失败。", worldSettings.Exception);
            }

            var sourcePath = worldSettings.Value.SaveFileLocation;
            if (!File.Exists(sourcePath))
            {
                return OperationResult<string>.Failed("当前存档文件不存在，无法备份。");
            }

            var backupRoot = Path.Combine(profile.DataPath, "BackupSaves");
            Directory.CreateDirectory(backupRoot);

            var sourceName = Path.GetFileNameWithoutExtension(sourcePath);
            var backupName = $"{WorkspaceLayout.SanitizeFileName(sourceName)}-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.vcdbs";
            var backupPath = Path.Combine(backupRoot, backupName);
            File.Copy(sourcePath, backupPath, overwrite: false);

            return OperationResult<string>.Success(backupPath, "备份完成。");
        }
        catch (Exception ex)
        {
            return OperationResult<string>.Failed("备份存档失败。", ex);
        }
    }
}
