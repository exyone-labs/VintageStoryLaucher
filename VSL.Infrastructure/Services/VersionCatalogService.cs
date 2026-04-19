using System.Globalization;
using System.Text.Json.Nodes;
using VSL.Application;
using VSL.Domain;
using VSL.Infrastructure.Storage;

namespace VSL.Infrastructure.Services;

public sealed class VersionCatalogService(HttpClient httpClient) : IVersionCatalogService
{
    private const string StableUnstableUrl = "https://api.vintagestory.at/stable-unstable.json";
    private static readonly TimeSpan CatalogRequestTimeout = TimeSpan.FromSeconds(12);

    public async Task<IReadOnlyList<ServerRelease>> GetOfficialReleasesAsync(CancellationToken cancellationToken = default)
    {
        WorkspaceLayout.EnsureWorkspaceExists();

        JsonObject rootObject;
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(CatalogRequestTimeout);

            using var stream = await httpClient.GetStreamAsync(StableUnstableUrl, timeoutCts.Token);
            var root = await JsonNode.ParseAsync(stream, cancellationToken: timeoutCts.Token);
            if (root is not JsonObject parsed)
            {
                return [];
            }

            rootObject = parsed;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Request timeout should not block the rest of the app workflow.
            return [];
        }
        catch (HttpRequestException)
        {
            // Offline / transient network errors: return empty list and keep launcher usable.
            return [];
        }

        var releases = new List<ServerRelease>();
        foreach (var versionNode in rootObject)
        {
            var version = versionNode.Key;
            if (versionNode.Value is not JsonObject versionObject)
            {
                continue;
            }

            if (!versionObject.TryGetPropertyValue("windowsserver", out var artifactNode) || artifactNode is not JsonObject artifactObject)
            {
                continue;
            }

            var filename = artifactObject["filename"]?.GetValue<string>();
            var md5 = artifactObject["md5"]?.GetValue<string>() ?? string.Empty;
            var latest = artifactObject["latest"]?.GetValue<int>() == 1;
            var url = artifactObject["urls"]?["cdn"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(filename) || string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            var fileSizeBytes = ParseFileSize(artifactObject["filesize"]?.GetValue<string>());
            var channel = url.Contains("/unstable/", StringComparison.OrdinalIgnoreCase)
                ? ReleaseChannel.Unstable
                : ReleaseChannel.Stable;

            releases.Add(new ServerRelease
            {
                Version = version,
                Channel = channel,
                Source = ReleaseSource.Official,
                FileName = filename,
                Url = url,
                Md5 = md5,
                FileSizeBytes = fileSizeBytes,
                IsLatest = latest,
                InstalledPath = Directory.Exists(WorkspaceLayout.GetServerInstallPath(version))
                    ? WorkspaceLayout.GetServerInstallPath(version)
                    : null
            });
        }

        return releases
            .OrderByDescending(static x => x.Channel == ReleaseChannel.Stable)
            .ThenByDescending(static x => x.Version, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<ServerRelease>> GetLocalReleasesAsync(CancellationToken cancellationToken = default)
    {
        WorkspaceLayout.EnsureWorkspaceExists();
        var cache = await JsonStorage.ReadOrDefaultAsync(WorkspaceLayout.VersionsCachePath, new VersionsCache(), cancellationToken);

        return cache.LocalReleases
            .Select(static local => new ServerRelease
            {
                Version = local.Version,
                Channel = ReleaseChannel.Local,
                Source = ReleaseSource.LocalImport,
                FileName = local.FileName,
                Url = local.ArchivePath,
                Md5 = local.Md5,
                FileSizeBytes = local.FileSizeBytes,
                IsLatest = false,
                InstalledPath = local.InstalledPath
            })
            .OrderByDescending(static x => x.Version, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task SaveLocalReleaseAsync(LocalReleaseRecord release, CancellationToken cancellationToken = default)
    {
        WorkspaceLayout.EnsureWorkspaceExists();
        var cache = await JsonStorage.ReadOrDefaultAsync(WorkspaceLayout.VersionsCachePath, new VersionsCache(), cancellationToken);
        cache.LastRefreshedUtc = DateTimeOffset.UtcNow;

        var existing = cache.LocalReleases.FirstOrDefault(x => x.Version.Equals(release.Version, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            cache.LocalReleases.Remove(existing);
        }

        cache.LocalReleases.Add(release);
        await JsonStorage.WriteAsync(WorkspaceLayout.VersionsCachePath, cache, cancellationToken);
    }

    private static long? ParseFileSize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Examples: "60.7 MB", "1.2 GB"
        var segments = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 2 || !double.TryParse(segments[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
        {
            return null;
        }

        return segments[1].ToUpperInvariant() switch
        {
            "KB" => (long)(numeric * 1024),
            "MB" => (long)(numeric * 1024 * 1024),
            "GB" => (long)(numeric * 1024 * 1024 * 1024),
            _ => null
        };
    }
}
