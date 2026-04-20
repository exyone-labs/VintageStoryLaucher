using VSL.Application;
using VSL.Domain;
using VSL.Infrastructure.Storage;

namespace VSL.Infrastructure.Services;

public sealed class LauncherSettingsService : ILauncherSettingsService
{
    public async Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        WorkspaceLayout.EnsureWorkspaceExists();

        var defaultSettings = new LauncherSettings
        {
            DataDirectory = WorkspaceLayout.DefaultDataRoot,
            SaveDirectory = WorkspaceLayout.DefaultSavesRoot,
            AutoStartWithWindows = false
        };

        var settings = await JsonStorage.ReadOrDefaultAsync(WorkspaceLayout.LauncherSettingsPath, defaultSettings, cancellationToken);
        settings.DataDirectory = NormalizeDirectory(settings.DataDirectory, WorkspaceLayout.DefaultDataRoot);
        settings.SaveDirectory = NormalizeDirectory(settings.SaveDirectory, WorkspaceLayout.DefaultSavesRoot);
        return settings;
    }

    public async Task<OperationResult> SaveAsync(LauncherSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            WorkspaceLayout.EnsureWorkspaceExists();

            var normalized = new LauncherSettings
            {
                DataDirectory = NormalizeDirectory(settings.DataDirectory, WorkspaceLayout.DefaultDataRoot),
                SaveDirectory = NormalizeDirectory(settings.SaveDirectory, WorkspaceLayout.DefaultSavesRoot),
                AutoStartWithWindows = settings.AutoStartWithWindows
            };

            await JsonStorage.WriteAsync(WorkspaceLayout.LauncherSettingsPath, normalized, cancellationToken);
            return OperationResult.Success("启动器设置已保存。");
        }
        catch (Exception ex)
        {
            return OperationResult.Failed("保存启动器设置失败。", ex);
        }
    }

    private static string NormalizeDirectory(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        try
        {
            return Path.GetFullPath(value.Trim());
        }
        catch
        {
            return fallback;
        }
    }
}
