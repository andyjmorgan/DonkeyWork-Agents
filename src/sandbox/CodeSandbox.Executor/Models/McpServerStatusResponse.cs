namespace CodeSandbox.Executor.Models;

public class McpServerStatusResponse
{
    public McpServerState State { get; set; }
    public string? Error { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? LastRequestAt { get; set; }
}
