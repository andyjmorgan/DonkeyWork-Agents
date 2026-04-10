namespace CodeSandbox.Contracts.Requests.Tools;

public class BashRequest
{
    public string Command { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int TimeoutSeconds { get; set; } = 120;
}
