using System.Text;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using Markdig;
using Markdig.Syntax;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

public sealed class MarkdownChunker : IMarkdownChunker
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public IReadOnlyList<string> Chunk(string markdown, ChunkerOptions options)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return Array.Empty<string>();
        }

        var blocks = ExtractBlocks(markdown);
        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var block in blocks)
        {
            if (block.Length > options.MaxCharCount)
            {
                FlushCurrent(current, chunks);
                foreach (var split in SplitOversizedBlock(block, options))
                {
                    chunks.Add(split);
                }
                continue;
            }

            var separator = current.Length == 0 ? string.Empty : "\n\n";
            var projected = current.Length + separator.Length + block.Length;

            if (projected > options.TargetCharCount && current.Length > 0)
            {
                FlushCurrent(current, chunks);
                current.Append(block);
            }
            else
            {
                current.Append(separator).Append(block);
            }
        }

        FlushCurrent(current, chunks);
        return chunks;
    }

    private static void FlushCurrent(StringBuilder buffer, List<string> chunks)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        chunks.Add(buffer.ToString().Trim());
        buffer.Clear();
    }

    private static IEnumerable<string> ExtractBlocks(string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);

        foreach (var block in document)
        {
            var text = Slice(markdown, block).Trim();
            if (text.Length > 0)
            {
                yield return text;
            }
        }
    }

    private static string Slice(string source, Block block)
    {
        var start = Math.Max(0, block.Span.Start);
        var end = Math.Min(source.Length, block.Span.End + 1);
        if (end <= start)
        {
            return string.Empty;
        }

        return source.Substring(start, end - start);
    }

    private static IEnumerable<string> SplitOversizedBlock(string block, ChunkerOptions options)
    {
        if (LooksLikeList(block))
        {
            foreach (var piece in GreedyJoin(SplitListItems(block), options))
            {
                yield return piece;
            }
            yield break;
        }

        foreach (var piece in GreedyJoin(SplitSentences(block), options))
        {
            yield return piece;
        }
    }

    private static bool LooksLikeList(string block)
    {
        using var reader = new StringReader(block);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.TrimStart();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ "))
            {
                return true;
            }

            if (trimmed.Length > 2 && char.IsDigit(trimmed[0]))
            {
                var dotIndex = trimmed.IndexOf('.');
                if (dotIndex > 0 && dotIndex < 4 && dotIndex + 1 < trimmed.Length && trimmed[dotIndex + 1] == ' ')
                {
                    return true;
                }
            }

            return false;
        }

        return false;
    }

    private static IEnumerable<string> SplitListItems(string block)
    {
        var current = new StringBuilder();
        using var reader = new StringReader(block);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.TrimStart();
            var isItemStart =
                trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ ") ||
                (trimmed.Length > 2 && char.IsDigit(trimmed[0]) && trimmed.IndexOf('.') is > 0 and <= 3);

            if (isItemStart && current.Length > 0)
            {
                yield return current.ToString().TrimEnd();
                current.Clear();
            }

            current.AppendLine(line);
        }

        if (current.Length > 0)
        {
            yield return current.ToString().TrimEnd();
        }
    }

    private static IEnumerable<string> SplitSentences(string paragraph)
    {
        var pieces = new List<string>();
        var current = new StringBuilder();

        for (var i = 0; i < paragraph.Length; i++)
        {
            current.Append(paragraph[i]);

            if (paragraph[i] is not ('.' or '?' or '!'))
            {
                continue;
            }

            var boundary = i + 1 >= paragraph.Length || char.IsWhiteSpace(paragraph[i + 1]);
            if (!boundary)
            {
                continue;
            }

            // swallow trailing whitespace up to and including the next newline or next word start
            while (i + 1 < paragraph.Length && char.IsWhiteSpace(paragraph[i + 1]))
            {
                current.Append(paragraph[i + 1]);
                i++;
            }

            pieces.Add(current.ToString().Trim());
            current.Clear();
        }

        if (current.Length > 0)
        {
            pieces.Add(current.ToString().Trim());
        }

        return pieces.Where(p => p.Length > 0);
    }

    private static IEnumerable<string> GreedyJoin(IEnumerable<string> parts, ChunkerOptions options)
    {
        var buffer = new StringBuilder();

        foreach (var part in parts)
        {
            if (part.Length > options.MaxCharCount)
            {
                if (buffer.Length > 0)
                {
                    yield return buffer.ToString().Trim();
                    buffer.Clear();
                }

                foreach (var slice in HardSplit(part, options.MaxCharCount))
                {
                    yield return slice;
                }
                continue;
            }

            var separator = buffer.Length == 0 ? string.Empty : "\n";
            if (buffer.Length + separator.Length + part.Length > options.TargetCharCount && buffer.Length > 0)
            {
                yield return buffer.ToString().Trim();
                buffer.Clear();
                buffer.Append(part);
            }
            else
            {
                buffer.Append(separator).Append(part);
            }
        }

        if (buffer.Length > 0)
        {
            yield return buffer.ToString().Trim();
        }
    }

    private static IEnumerable<string> HardSplit(string value, int maxLength)
    {
        for (var start = 0; start < value.Length; start += maxLength)
        {
            var length = Math.Min(maxLength, value.Length - start);
            yield return value.Substring(start, length).Trim();
        }
    }
}
