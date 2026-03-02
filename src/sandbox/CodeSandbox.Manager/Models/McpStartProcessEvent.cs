namespace CodeSandbox.Manager.Models;

public class McpStartProcessEvent
{
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
}
