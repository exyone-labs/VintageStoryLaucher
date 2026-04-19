using VSL.Domain;

namespace VSL.Tests.TestSupport;

internal static class TestWorkspace
{
    public static string CreateProfileDataPath()
    {
        var root = Path.Combine(WorkspaceLayout.WorkspaceRoot, "tests", Guid.NewGuid().ToString("N"));
        var dataPath = Path.Combine(root, "data");
        Directory.CreateDirectory(dataPath);
        return dataPath;
    }

    public static void CleanupProfileDataPath(string dataPath)
    {
        var root = Directory.GetParent(dataPath)?.FullName;
        if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    public static string CreateTempFilePath(string extension)
    {
        Directory.CreateDirectory(Path.Combine(WorkspaceLayout.WorkspaceRoot, "tests-temp"));
        return Path.Combine(WorkspaceLayout.WorkspaceRoot, "tests-temp", $"{Guid.NewGuid():N}{extension}");
    }
}
