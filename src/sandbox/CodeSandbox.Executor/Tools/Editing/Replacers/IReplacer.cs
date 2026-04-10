namespace CodeSandbox.Executor.Tools.Editing.Replacers;

public interface IReplacer
{
    IEnumerable<string> FindMatches(string content, string find);
}
