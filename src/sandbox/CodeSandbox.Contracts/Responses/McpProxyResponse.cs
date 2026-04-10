namespace CodeSandbox.Contracts.Responses;

public class McpProxyResponse
{
    public string Body { get; set; } = string.Empty;

    public bool IsNotification { get; set; }
}
