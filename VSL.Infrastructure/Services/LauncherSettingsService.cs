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
        settings.Vs2QQOneBotWsUrl = NormalizeTextOrDefault(settings.Vs2QQOneBotWsUrl, "ws://127.0.0.1:3001/");
        settings.Vs2QQAccessToken = NormalizeTextOrDefault(settings.Vs2QQAccessToken, string.Empty);
        settings.Vs2QQReconnectIntervalSec = NormalizePositiveInt(settings.Vs2QQReconnectIntervalSec, 5);
        settings.Vs2QQDatabasePath = NormalizePathOrDefault(settings.Vs2QQDatabasePath, BuildDefaultVs2QQDatabasePath());
        settings.Vs2QQPollIntervalSec = NormalizePositiveDouble(settings.Vs2QQPollIntervalSec, 1.0);
        settings.Vs2QQDefaultEncoding = NormalizeTextOrDefault(settings.Vs2QQDefaultEncoding, "utf-8");
        settings.Vs2QQFallbackEncoding = NormalizeTextOrDefault(settings.Vs2QQFallbackEncoding, "gbk");
        settings.Vs2QQSuperUsers = NormalizeSuperUsers(settings.Vs2QQSuperUsers);
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
                AutoStartWithWindows = settings.AutoStartWithWindows,
                Vs2QQOneBotWsUrl = NormalizeTextOrDefault(settings.Vs2QQOneBotWsUrl, "ws://127.0.0.1:3001/"),
                Vs2QQAccessToken = NormalizeTextOrDefault(settings.Vs2QQAccessToken, string.Empty),
                Vs2QQReconnectIntervalSec = NormalizePositiveInt(settings.Vs2QQReconnectIntervalSec, 5),
                Vs2QQDatabasePath = NormalizePathOrDefault(settings.Vs2QQDatabasePath, BuildDefaultVs2QQDatabasePath()),
                Vs2QQPollIntervalSec = NormalizePositiveDouble(settings.Vs2QQPollIntervalSec, 1.0),
                Vs2QQDefaultEncoding = NormalizeTextOrDefault(settings.Vs2QQDefaultEncoding, "utf-8"),
                Vs2QQFallbackEncoding = NormalizeTextOrDefault(settings.Vs2QQFallbackEncoding, "gbk"),
                Vs2QQSuperUsers = NormalizeSuperUsers(settings.Vs2QQSuperUsers)
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

    private static string NormalizePathOrDefault(string? value, string fallback)
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

    private static string NormalizeTextOrDefault(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static int NormalizePositiveInt(int value, int fallback)
    {
        return value <= 0 ? fallback : value;
    }

    private static double NormalizePositiveDouble(double value, double fallback)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            return fallback;
        }

        return value;
    }

    private static List<long> NormalizeSuperUsers(IEnumerable<long>? source)
    {
        if (source is null)
        {
            return [];
        }

        return source.Where(x => x > 0).Distinct().Order().ToList();
    }

    private static string BuildDefaultVs2QQDatabasePath()
    {
        return Path.Combine(WorkspaceLayout.WorkspaceRoot, "vs2qq", "vs2qq.db");
    }
}
