namespace CodeSandbox.Manager.Models.Api;

public record ExecuteCommandApiRequest(string Command, int TimeoutSeconds = 300);
