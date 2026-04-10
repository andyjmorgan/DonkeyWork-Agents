namespace CodeSandbox.Contracts.Requests;

public class McpProxyRequest
{
    public string Body { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;
}
