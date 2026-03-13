namespace CodeSandbox.Manager.Models.Api;

public record StartMcpProcessApiRequest(
    string Command,
    string[]? Args = null,
    string[]? PreExecScripts = null,
    int Timeout = 30,
    string? WorkDir = null);
