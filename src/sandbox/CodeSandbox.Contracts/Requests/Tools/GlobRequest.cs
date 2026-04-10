namespace CodeSandbox.Contracts.Requests.Tools;

public class GlobRequest
{
    public string Pattern { get; set; } = string.Empty;

    public string? Path { get; set; }
}
