namespace CodeSandbox.Executor.Tools.Editing.Replacers.Whitespace;

public class LineTrimmedReplacer : IReplacer
{
    public IEnumerable<string> FindMatches(string content, string find)
    {
        var originalLines = content.Split('\n');
        var searchLines = find.Split('\n').ToList();

        if (searchLines.Count > 0 && searchLines[^1] == string.Empty)
        {
            searchLines.RemoveAt(searchLines.Count - 1);
        }

        for (int i = 0; i <= originalLines.Length - searchLines.Count; i++)
        {
            bool matches = true;
            for (int j = 0; j < searchLines.Count; j++)
            {
                if (originalLines[i + j].Trim() != searchLines[j].Trim())
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                int matchStartIndex = 0;
                for (int k = 0; k < i; k++)
                {
                    matchStartIndex += originalLines[k].Length + 1;
                }

                int matchEndIndex = matchStartIndex;
                for (int k = 0; k < searchLines.Count; k++)
                {
                    matchEndIndex += originalLines[i + k].Length;
                    if (k < searchLines.Count - 1)
                    {
                        matchEndIndex += 1;
                    }
                }

                yield return content.Substring(matchStartIndex, matchEndIndex - matchStartIndex);
            }
        }
    }
}
