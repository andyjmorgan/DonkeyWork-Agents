namespace CodeSandbox.Executor.Tools.Search;

public interface IRipgrepProvider
{
    string GetRipgrepPath();

    IAsyncEnumerable<string> ListFiles(
        string cwd,
        string[]? globs = null,
        bool hidden = true,
        int? maxDepth = null,
        CancellationToken ct = default);

    Task<List<GrepMatch>> Search(
        string cwd,
        string pattern,
        string[]? globs = null,
        int? maxCount = null,
        CancellationToken ct = default);
}
