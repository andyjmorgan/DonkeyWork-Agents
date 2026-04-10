using System.Text;
using CodeSandbox.Executor.Tools.FileSystem;

namespace CodeSandbox.Executor.Tools.Reading;

public sealed class ReadTool
{
    public const int DefaultReadLimit = 2000;
    public const int MaxLineLength = 2000;
    public const int MaxBytes = 50 * 1024;

    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".tar", ".gz", ".exe", ".dll", ".so", ".class", ".jar", ".war",
        ".7z", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".odt", ".ods", ".odp", ".bin", ".dat", ".obj", ".o", ".a",
        ".lib", ".wasm", ".pyc", ".pyo",
    };

    private readonly IFileSystem fs;

    public ReadTool(IFileSystem? fileSystem = null)
    {
        this.fs = fileSystem ?? new PhysicalFileSystem();
    }

    public ToolResult Execute(string filePath, int? offset = null, int? limit = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("filePath is required.", nameof(filePath));
        }

        if (this.fs.DirectoryExists(filePath))
        {
            return this.ReadDirectory(filePath, offset, limit ?? DefaultReadLimit);
        }

        if (!this.fs.FileExists(filePath))
        {
            return this.HandleFileNotFound(filePath);
        }

        var ext = Path.GetExtension(filePath);
        if (BinaryExtensions.Contains(ext))
        {
            throw new InvalidOperationException(
                $"Cannot read binary file (detected by extension '{ext}'): {filePath}");
        }

        this.DetectBinaryByContent(filePath);

        return this.ReadTextFile(filePath, offset, limit ?? DefaultReadLimit);
    }

    private ToolResult ReadDirectory(string dirPath, int? offset, int limit)
    {
        var entries = this.fs.GetDirectoryEntries(dirPath);

        var names = entries
            .Select(e =>
            {
                var name = this.fs.GetFileName(e);
                return this.fs.IsDirectory(e) ? name + "/" : name;
            })
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        int startIndex = 0;
        if (offset.HasValue)
        {
            startIndex = offset.Value - 1;
            if (startIndex < 0)
            {
                startIndex = 0;
            }
        }

        var page = names.Skip(startIndex).Take(limit).ToList();

        var sb = new StringBuilder();
        foreach (var name in page)
        {
            sb.AppendLine(name);
        }

        var truncated = startIndex + page.Count < names.Count;
        if (truncated)
        {
            sb.Append($"({page.Count} of {names.Count} entries)");
        }
        else
        {
            sb.Append($"({names.Count} entries)");
        }

        return new ToolResult
        {
            Title = $"Directory listing: {dirPath}",
            Output = sb.ToString(),
            Truncated = truncated,
        };
    }

    private void DetectBinaryByContent(string filePath)
    {
        const int sampleSize = 4096;
        var buffer = new byte[sampleSize];
        int bytesRead;

        using (var stream = this.fs.OpenRead(filePath))
        {
            bytesRead = stream.Read(buffer, 0, sampleSize);
        }

        if (bytesRead == 0)
        {
            return;
        }

        int nullCount = 0;
        int nonPrintableCount = 0;

        for (int i = 0; i < bytesRead; i++)
        {
            byte b = buffer[i];
            if (b == 0)
            {
                nullCount++;
            }

            if ((b <= 0x08) || (b >= 0x0E && b <= 0x1F))
            {
                nonPrintableCount++;
            }
        }

        if (nullCount > 0)
        {
            throw new InvalidOperationException(
                $"Cannot read binary file (null bytes detected): {filePath}");
        }

        double ratio = (double)nonPrintableCount / bytesRead;
        if (ratio > 0.3)
        {
            throw new InvalidOperationException(
                $"Cannot read binary file (>30% non-printable characters): {filePath}");
        }
    }

    private ToolResult ReadTextFile(string filePath, int? offset, int limit)
    {
        int startLine = offset ?? 1;
        if (startLine < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                "Offset must be >= 1 (1-indexed line number).");
        }

        var sb = new StringBuilder();

        int currentLine = 0;
        int linesEmitted = 0;
        int totalBytes = 0;
        bool truncatedByBytes = false;
        bool truncatedByLimit = false;
        int totalLines = 0;

        using (var stream = this.fs.OpenRead(filePath))
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                currentLine++;
                totalLines = currentLine;

                if (currentLine < startLine)
                {
                    continue;
                }

                if (linesEmitted >= limit)
                {
                    truncatedByLimit = true;
                    while (reader.ReadLine() != null)
                    {
                        totalLines++;
                    }

                    break;
                }

                if (line.Length > MaxLineLength)
                {
                    line = line[..MaxLineLength] + $"... (line truncated to {MaxLineLength} chars)";
                }

                var formatted = $"{currentLine,6}\u2192{line}";
                int lineBytes = Encoding.UTF8.GetByteCount(formatted) + 1;

                if (totalBytes + lineBytes > MaxBytes && linesEmitted > 0)
                {
                    truncatedByBytes = true;
                    while (reader.ReadLine() != null)
                    {
                        totalLines++;
                    }

                    break;
                }

                sb.AppendLine(formatted);
                totalBytes += lineBytes;
                linesEmitted++;
            }
        }

        bool truncated = truncatedByBytes || truncatedByLimit;
        int endLine = (startLine - 1) + linesEmitted;
        int nextOffset = endLine + 1;

        if (truncatedByBytes)
        {
            sb.AppendLine();
            sb.Append($"(Output capped at 50 KB. Showing lines {startLine}-{endLine}. Use offset={nextOffset} to continue.)");
        }
        else if (truncatedByLimit)
        {
            sb.AppendLine();
            sb.Append($"(Showing lines {startLine}-{endLine} of {totalLines}. Use offset={nextOffset} to continue.)");
        }
        else
        {
            sb.AppendLine();
            sb.Append($"(End of file - total {totalLines} lines)");
        }

        if (linesEmitted == 0 && totalLines > 0 && startLine > totalLines)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                $"Offset {startLine} is beyond end of file ({totalLines} lines).");
        }

        return new ToolResult
        {
            Title = $"Read file: {filePath}",
            Output = sb.ToString(),
            Truncated = truncated,
            Metadata = new Dictionary<string, object>
            {
                ["startLine"] = startLine,
                ["endLine"] = endLine,
                ["totalLines"] = totalLines,
            },
        };
    }

    private ToolResult HandleFileNotFound(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var dirName = this.fs.GetDirectoryName(filePath);
        var suggestions = new List<string>();

        if (dirName != null && this.fs.DirectoryExists(dirName))
        {
            try
            {
                var siblings = this.fs.GetDirectoryEntries(dirName)
                    .Where(e => !this.fs.IsDirectory(e))
                    .Select(e => this.fs.GetFileName(e))
                    .ToList();

                suggestions = siblings
                    .Select(s => (name: s, dist: LevenshteinDistance(
                        fileName.ToLowerInvariant(), s.ToLowerInvariant())))
                    .OrderBy(x => x.dist)
                    .Take(3)
                    .Where(x => x.dist <= Math.Max(fileName.Length, x.name.Length) / 2)
                    .Select(x => Path.Combine(dirName, x.name))
                    .ToList();
            }
            catch
            {
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"File not found: {filePath}");
        if (suggestions.Count > 0)
        {
            sb.AppendLine("Did you mean:");
            foreach (var s in suggestions)
            {
                sb.AppendLine($"  - {s}");
            }
        }

        return new ToolResult
        {
            Title = "File not found",
            Output = sb.ToString().TrimEnd(),
        };
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int[,] d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++)
        {
            d[i, 0] = i;
        }

        for (int j = 0; j <= b.Length; j++)
        {
            d[0, j] = j;
        }

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[a.Length, b.Length];
    }
}
