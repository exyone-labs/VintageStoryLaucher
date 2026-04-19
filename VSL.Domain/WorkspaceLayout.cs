using System.Text.RegularExpressions;

namespace VSL.Domain;

public static partial class WorkspaceLayout
{
    public const string PreferredWorkspaceRoot = @"E:\vintagestory\VSL\workspace";

    public static string WorkspaceRoot { get; } = ResolveWorkspaceRoot();

    public static string VersionsCachePath => Path.Combine(WorkspaceRoot, "versions-cache.json");

    public static string ProfilesPath => Path.Combine(WorkspaceRoot, "profiles.json");

    public static string TempPath => Path.Combine(WorkspaceRoot, ".tmp");

    public static string PackagesPath => Path.Combine(WorkspaceRoot, "packages");

    public static string ServersRoot => Path.Combine(WorkspaceRoot, "servers", "windows");

    public static string ProfilesRoot => Path.Combine(WorkspaceRoot, "profiles");

    public static string GetServerInstallPath(string version) => Path.Combine(ServersRoot, version);

    public static string GetProfileRoot(string profileId) => Path.Combine(ProfilesRoot, profileId);

    public static string GetProfileDataPath(string profileId) => Path.Combine(GetProfileRoot(profileId), "data");

    public static string GetServerConfigPath(string dataPath) => Path.Combine(dataPath, "serverconfig.json");

    public static string GetSavesPath(string dataPath) => Path.Combine(dataPath, "Saves");

    public static string GetModsPath(string dataPath) => Path.Combine(dataPath, "Mods");

    public static string GetLogsPath(string dataPath) => Path.Combine(dataPath, "Logs");

    public static string GetServerMainLogPath(string dataPath) => Path.Combine(GetLogsPath(dataPath), "server-main.log");

    public static string GetDefaultSaveFile(string dataPath) => Path.Combine(GetSavesPath(dataPath), "default.vcdbs");

    public static void EnsureWorkspaceExists()
    {
        Directory.CreateDirectory(WorkspaceRoot);
        Directory.CreateDirectory(TempPath);
        Directory.CreateDirectory(PackagesPath);
        Directory.CreateDirectory(ServersRoot);
        Directory.CreateDirectory(ProfilesRoot);
    }

    public static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join('_', name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
    }

    public static string TryExtractVersionFromServerFileName(string fileName)
    {
        // Example: vs_server_win-x64_1.21.7.zip
        var match = VersionPattern().Match(fileName);
        return match.Success ? match.Groups["version"].Value : "local-" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private static string ResolveWorkspaceRoot()
    {
        var candidateRoots = new[]
        {
            PreferredWorkspaceRoot,
            Path.Combine(AppContext.BaseDirectory, "workspace"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VSL", "workspace")
        };

        foreach (var candidate in candidateRoots)
        {
            try
            {
                Directory.CreateDirectory(candidate);
                return candidate;
            }
            catch
            {
                // Try next candidate.
            }
        }

        // Last resort.
        return Path.Combine(Path.GetTempPath(), "VSL", "workspace");
    }

    [GeneratedRegex(@"vs_server_win-x64_(?<version>[^\.]+\.\d+\.\d+(?:-[^\.]+(?:\.\d+)?)?)\.zip", RegexOptions.IgnoreCase)]
    private static partial Regex VersionPattern();
}
