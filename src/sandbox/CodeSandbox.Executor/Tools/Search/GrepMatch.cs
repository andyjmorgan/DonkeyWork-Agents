namespace CodeSandbox.Executor.Tools.Search;

public record GrepMatch(string FilePath, int LineNumber, string LineText, DateTime ModifiedTime);
