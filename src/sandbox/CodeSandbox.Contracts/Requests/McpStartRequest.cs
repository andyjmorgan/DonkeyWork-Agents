namespace CodeSandbox.Contracts.Requests;

public class McpStartRequest
{
    public string Command { get; set; } = string.Empty;

    public string[] Arguments { get; set; } = [];

    public string[] PreExecScripts { get; set; } = [];

    public int TimeoutSeconds { get; set; } = 30;

    public string? WorkingDirectory { get; set; }
}
