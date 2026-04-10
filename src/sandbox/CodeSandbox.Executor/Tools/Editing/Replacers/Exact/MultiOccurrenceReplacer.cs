namespace CodeSandbox.Executor.Tools.Editing.Replacers.Exact;

public class MultiOccurrenceReplacer : IReplacer
{
    public IEnumerable<string> FindMatches(string content, string find)
    {
        int startIndex = 0;
        while (true)
        {
            int index = content.IndexOf(find, startIndex, StringComparison.Ordinal);
            if (index == -1)
            {
                break;
            }

            yield return find;
            startIndex = index + find.Length;
        }
    }
}
