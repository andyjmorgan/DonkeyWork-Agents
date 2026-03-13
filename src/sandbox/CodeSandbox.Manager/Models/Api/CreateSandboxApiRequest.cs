namespace CodeSandbox.Manager.Models.Api;

public record CreateSandboxApiRequest(
    string UserId,
    string ConversationId,
    Dictionary<string, string>? EnvVars = null,
    List<string>? DynamicCredentialDomains = null);
