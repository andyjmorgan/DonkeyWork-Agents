using System.Text;

namespace CodeSandbox.Executor.Tools.Search;

public sealed class GrepTool
{
    private const int MaxMatches = 250;
    private const int MaxLineLength = 2000;

    private readonly IRipgrepProvider ripgrep;

    public GrepTool(IRipgrepProvider ripgrep)
    {
        this.ripgrep = ripgrep;
    }

    public async Task<ToolResult> RunAsync(
        string pattern,
        string? path = null,
        string? include = null,
        CancellationToken ct = default)
    {
        var cwd = path ?? Directory.GetCurrentDirectory();

        string[]? globs = include is not null ? new[] { include } : null;

        var matches = await this.ripgrep.Search(cwd, pattern, globs, ct: ct);

        if (matches.Count == 0)
        {
            return new ToolResult
            {
                Title = "Grep",
                Output = "No files found",
            };
        }

        matches.Sort((a, b) => b.ModifiedTime.CompareTo(a.ModifiedTime));

        var totalMatchCount = matches.Count;
        var truncated = totalMatchCount > MaxMatches;
        if (truncated)
        {
            matches = matches.GetRange(0, MaxMatches);
        }

        var grouped = new List<(string File, List<GrepMatch> Matches)>();
        var fileIndex = new Dictionary<string, int>();

        foreach (var m in matches)
        {
            if (!fileIndex.TryGetValue(m.FilePath, out var idx))
            {
                idx = grouped.Count;
                fileIndex[m.FilePath] = idx;
                grouped.Add((m.FilePath, new List<GrepMatch>()));
            }

            grouped[idx].Matches.Add(m);
        }

        var sb = new StringBuilder();

        foreach (var (file, fileMatches) in grouped)
        {
            sb.AppendLine(file);

            fileMatches.Sort((a, b) => a.LineNumber.CompareTo(b.LineNumber));

            foreach (var m in fileMatches)
            {
                var lineText = m.LineText;
                if (lineText.Length > MaxLineLength)
                {
                    lineText = lineText[..MaxLineLength] + "...";
                }

                sb.AppendLine($"  Line {m.LineNumber}: {lineText}");
            }

            sb.AppendLine();
        }

        if (truncated)
        {
            sb.AppendLine($"(Results truncated: showing {MaxMatches} of {totalMatchCount} matches. Consider using a more specific path or pattern.)");
        }

        return new ToolResult
        {
            Title = "Grep",
            Output = sb.ToString().TrimEnd(),
            Truncated = truncated,
            Metadata = new Dictionary<string, object>
            {
                ["matchCount"] = totalMatchCount,
                ["fileCount"] = grouped.Count,
            },
        };
    }
}
