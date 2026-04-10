using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace CodeSandbox.Executor.Tools.Search;

public sealed class RipgrepProvider : IRipgrepProvider
{
    private const char FieldSeparator = '\x1f';

    private string? cachedPath;

    public string GetRipgrepPath()
    {
        if (this.cachedPath is not null)
        {
            return this.cachedPath;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "which",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("rg");

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start 'which' process.");

        var path = proc.StandardOutput.ReadToEnd().Trim();
        proc.WaitForExit();

        if (proc.ExitCode != 0 || string.IsNullOrEmpty(path))
        {
            throw new FileNotFoundException(
                "ripgrep (rg) not found. Ensure ripgrep is installed and available on PATH.");
        }

        this.cachedPath = path;
        return path;
    }

    public async IAsyncEnumerable<string> ListFiles(
        string cwd,
        string[]? globs = null,
        bool hidden = true,
        int? maxDepth = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var rgPath = this.GetRipgrepPath();

        var psi = new ProcessStartInfo
        {
            FileName = rgPath,
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };

        psi.ArgumentList.Add("--files");

        if (hidden)
        {
            psi.ArgumentList.Add("--hidden");
        }

        psi.ArgumentList.Add("--glob=!.git/*");

        if (globs is not null)
        {
            foreach (var g in globs)
            {
                psi.ArgumentList.Add($"--glob={g}");
            }
        }

        if (maxDepth.HasValue)
        {
            psi.ArgumentList.Add($"--max-depth={maxDepth.Value}");
        }

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start ripgrep process.");

        while (await proc.StandardOutput.ReadLineAsync(ct) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return Path.GetFullPath(Path.Combine(cwd, line));
            }
        }

        await proc.WaitForExitAsync(ct);
    }

    public async Task<List<GrepMatch>> Search(
        string cwd,
        string pattern,
        string[]? globs = null,
        int? maxCount = null,
        CancellationToken ct = default)
    {
        var rgPath = this.GetRipgrepPath();

        var psi = new ProcessStartInfo
        {
            FileName = rgPath,
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };

        psi.ArgumentList.Add("-nH");
        psi.ArgumentList.Add("--hidden");
        psi.ArgumentList.Add("--no-messages");
        psi.ArgumentList.Add($"--field-match-separator={FieldSeparator}");

        if (globs is not null)
        {
            foreach (var g in globs)
            {
                psi.ArgumentList.Add($"--glob={g}");
            }
        }

        if (maxCount.HasValue)
        {
            psi.ArgumentList.Add($"--max-count={maxCount.Value}");
        }

        psi.ArgumentList.Add("--regexp");
        psi.ArgumentList.Add(pattern);

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start ripgrep process.");

        var results = new List<GrepMatch>();
        var mtimeCache = new Dictionary<string, DateTime>();

        while (await proc.StandardOutput.ReadLineAsync(ct) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var firstSep = line.IndexOf(FieldSeparator);
            if (firstSep < 0)
            {
                continue;
            }

            var secondSep = line.IndexOf('\x1f', firstSep + 1);
            if (secondSep < 0)
            {
                continue;
            }

            var filePath = Path.GetFullPath(Path.Combine(cwd, line[..firstSep]));
            var lineNumStr = line[(firstSep + 1)..secondSep];
            var lineText = line[(secondSep + 1)..];

            if (!int.TryParse(lineNumStr, out var lineNum))
            {
                continue;
            }

            if (!mtimeCache.TryGetValue(filePath, out var mtime))
            {
                try
                {
                    mtime = File.GetLastWriteTimeUtc(filePath);
                }
                catch
                {
                    mtime = DateTime.MinValue;
                }

                mtimeCache[filePath] = mtime;
            }

            results.Add(new GrepMatch(filePath, lineNum, lineText, mtime));
        }

        await proc.WaitForExitAsync(ct);

        return results;
    }
}
