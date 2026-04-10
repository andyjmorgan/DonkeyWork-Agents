namespace CodeSandbox.Contracts.Requests.Tools;

public class WriteRequest
{
    public string FilePath { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}
