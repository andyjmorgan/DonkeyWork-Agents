using System.Text;

namespace CodeSandbox.Executor.Tools.Search;

public sealed class GlobTool
{
    private const int MaxFiles = 100;

    private readonly IRipgrepProvider ripgrep;

    public GlobTool(IRipgrepProvider ripgrep)
    {
        this.ripgrep = ripgrep;
    }

    public async Task<ToolResult> RunAsync(
        string pattern,
        string? path = null,
        CancellationToken ct = default)
    {
        var cwd = path ?? Directory.GetCurrentDirectory();

        var files = new List<(string Path, DateTime Mtime)>();
        var truncated = false;

        await foreach (var filePath in this.ripgrep.ListFiles(cwd, new[] { pattern }, ct: ct))
        {
            DateTime mtime;
            try
            {
                mtime = File.GetLastWriteTimeUtc(filePath);
            }
            catch
            {
                mtime = DateTime.MinValue;
            }

            files.Add((filePath, mtime));

            if (files.Count > MaxFiles)
            {
                truncated = true;
                break;
            }
        }

        if (files.Count == 0)
        {
            return new ToolResult
            {
                Title = "Glob",
                Output = "No files found",
            };
        }

        files.Sort((a, b) => b.Mtime.CompareTo(a.Mtime));

        if (files.Count > MaxFiles)
        {
            files.RemoveRange(MaxFiles, files.Count - MaxFiles);
            truncated = true;
        }

        var sb = new StringBuilder();
        foreach (var (filePath, _) in files)
        {
            sb.AppendLine(filePath);
        }

        if (truncated)
        {
            sb.AppendLine("(Results are truncated. Consider using a more specific path or pattern.)");
        }

        return new ToolResult
        {
            Title = "Glob",
            Output = sb.ToString().TrimEnd(),
            Truncated = truncated,
            Metadata = new Dictionary<string, object>
            {
                ["fileCount"] = files.Count,
            },
        };
    }
}
