namespace CodeSandbox.Contracts.Responses;

public class ToolResponse
{
    public string Output { get; set; } = string.Empty;

    public bool IsError { get; set; }
}
