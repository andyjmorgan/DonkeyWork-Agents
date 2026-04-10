namespace CodeSandbox.Contracts.Responses;

public class McpStatusResponse
{
    public string State { get; set; } = string.Empty;

    public string? Error { get; set; }

    public string? StartedAt { get; set; }

    public string? LastRequestAt { get; set; }
}
