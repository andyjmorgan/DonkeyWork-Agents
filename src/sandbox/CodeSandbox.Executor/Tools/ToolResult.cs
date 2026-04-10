namespace CodeSandbox.Executor.Tools;

public class ToolResult
{
    public required string Title { get; init; }

    public required string Output { get; init; }

    public Dictionary<string, object>? Metadata { get; init; }

    public bool Truncated { get; init; }
}
