namespace CodeSandbox.Manager.Models.Api;

public record CreateMcpServerApiRequest(
    string UserId,
    string ConfigId,
    string? Command = null,
    string[]? Args = null,
    string[]? PreExecScripts = null,
    int Timeout = 30,
    Dictionary<string, string>? EnvVars = null,
    string? WorkDir = null);
