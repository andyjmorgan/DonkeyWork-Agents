namespace CodeSandbox.Manager.Models;

public class ExecutionRequest
{
    public string Command { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 300;
}
