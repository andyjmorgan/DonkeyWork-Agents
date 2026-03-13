namespace CodeSandbox.Manager.Models.Api;

public record ProxyMcpApiRequest(string Body, int TimeoutSeconds = 30);
