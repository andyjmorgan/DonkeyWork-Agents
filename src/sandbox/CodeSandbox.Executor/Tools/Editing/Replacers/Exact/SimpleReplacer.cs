namespace CodeSandbox.Executor.Tools.Editing.Replacers.Exact;

public class SimpleReplacer : IReplacer
{
    public IEnumerable<string> FindMatches(string content, string find)
    {
        if (content.Contains(find))
        {
            yield return find;
        }
    }
}
