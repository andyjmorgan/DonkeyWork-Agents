namespace CodeSandbox.Executor.Tools.FileSystem;

public interface IFileSystem
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    Stream OpenRead(string path);

    string[] GetDirectoryEntries(string path);

    bool IsDirectory(string entryPath);

    string GetFileName(string path);

    string? GetDirectoryName(string path);
}
