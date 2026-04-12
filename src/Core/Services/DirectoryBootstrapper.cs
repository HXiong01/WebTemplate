namespace WebTemplate.Core.Services;

public static class DirectoryBootstrapper
{
    public static string EnsureStorageDirectories(string storageRoot)
    {
        var fullRoot = Path.GetFullPath(storageRoot);
        Directory.CreateDirectory(fullRoot);
        Directory.CreateDirectory(Path.Combine(fullRoot, "logs"));
        Directory.CreateDirectory(Path.Combine(fullRoot, "artifacts"));
        return fullRoot;
    }
}
