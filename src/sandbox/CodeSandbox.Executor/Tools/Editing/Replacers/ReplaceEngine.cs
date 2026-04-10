using CodeSandbox.Executor.Tools.Editing.Replacers.Exact;
using CodeSandbox.Executor.Tools.Editing.Replacers.Whitespace;

namespace CodeSandbox.Executor.Tools.Editing.Replacers;

public class ReplaceEngine
{
    private static readonly IReplacer[] Replacers =
    [
        new SimpleReplacer(),
        new LineTrimmedReplacer(),
        new MultiOccurrenceReplacer(),
    ];

    public static string Replace(string content, string oldString, string newString, bool replaceAll = false)
    {
        if (oldString == newString)
        {
            throw new InvalidOperationException("No changes to apply: oldString and newString are identical.");
        }

        foreach (var replacer in Replacers)
        {
            var matches = replacer.FindMatches(content, oldString)
                .Where(s => content.Contains(s, StringComparison.Ordinal))
                .ToList();

            if (matches.Count == 0)
            {
                continue;
            }

            var search = matches[0];
            var adjustedNew = search != oldString
                ? AdjustIndentation(newString, oldString, search)
                : newString;

            if (replaceAll)
            {
                return content.Replace(search, adjustedNew);
            }

            bool hasMultiple = matches.Count > 1
                || content.IndexOf(search, StringComparison.Ordinal)
                   != content.LastIndexOf(search, StringComparison.Ordinal);

            if (hasMultiple)
            {
                throw new InvalidOperationException(
                    "Found multiple matches for oldString. Provide more surrounding context to make the match unique.");
            }

            int index = content.IndexOf(search, StringComparison.Ordinal);
            return content[..index] + adjustedNew + content[(index + search.Length)..];
        }

        throw new InvalidOperationException(
            "Could not find oldString in the file. It must match exactly, including whitespace, indentation, and line endings.");
    }

    internal static string AdjustIndentation(string newString, string oldString, string search)
    {
        int oldIndent = GetMinIndent(oldString);
        int searchIndent = GetMinIndent(search);
        int delta = searchIndent - oldIndent;

        if (delta == 0)
        {
            return newString;
        }

        var lines = newString.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().Length == 0)
            {
                continue;
            }

            if (delta > 0)
            {
                lines[i] = new string(' ', delta) + lines[i];
            }
            else
            {
                int toRemove = Math.Min(-delta, GetLeadingWhitespaceCount(lines[i]));
                lines[i] = lines[i][toRemove..];
            }
        }

        return string.Join("\n", lines);
    }

    private static int GetMinIndent(string text)
    {
        int min = int.MaxValue;
        foreach (var line in text.Split('\n'))
        {
            if (line.Trim().Length == 0)
            {
                continue;
            }

            int count = GetLeadingWhitespaceCount(line);
            if (count < min)
            {
                min = count;
            }
        }

        return min == int.MaxValue ? 0 : min;
    }

    private static int GetLeadingWhitespaceCount(string line)
    {
        int count = 0;
        foreach (char c in line)
        {
            if (c == ' ' || c == '\t')
            {
                count++;
            }
            else
            {
                break;
            }
        }

        return count;
    }
}
