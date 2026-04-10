namespace CodeSandbox.Contracts.Requests.Tools;

public class GrepRequest
{
    public string Pattern { get; set; } = string.Empty;

    public string? Path { get; set; }

    public string? Include { get; set; }
}
