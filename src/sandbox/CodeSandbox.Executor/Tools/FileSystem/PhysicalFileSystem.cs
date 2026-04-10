namespace CodeSandbox.Executor.Tools.FileSystem;

public sealed class PhysicalFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public Stream OpenRead(string path) => File.OpenRead(path);

    public string[] GetDirectoryEntries(string path) =>
        Directory.GetFileSystemEntries(path);

    public bool IsDirectory(string entryPath) =>
        Directory.Exists(entryPath);

    public string GetFileName(string path) => Path.GetFileName(path);

    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);
}
