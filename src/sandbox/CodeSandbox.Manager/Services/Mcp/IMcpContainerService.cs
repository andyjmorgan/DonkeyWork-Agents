using CodeSandbox.Manager.Models;

namespace CodeSandbox.Manager.Services.Mcp;

public interface IMcpContainerService
{
    // Pod lifecycle
    IAsyncEnumerable<ContainerCreationEvent> CreateMcpServerAsync(CreateMcpServerRequest request, CancellationToken ct = default);
    Task<McpServerInfo?> FindMcpServerAsync(string userId, string mcpServerConfigId, CancellationToken ct = default);
    Task<McpServerInfo?> GetMcpServerAsync(string podName, CancellationToken ct = default);
    Task<List<McpServerInfo>> ListMcpServersAsync(CancellationToken ct = default);
    Task<DeleteSandboxResponse> DeleteMcpServerAsync(string podName, CancellationToken ct = default);

    // MCP process management (gRPC to pod)
    IAsyncEnumerable<McpStartProcessEvent> StartMcpProcessAsync(string podName, McpStartRequest request, CancellationToken ct = default);
    Task<McpProxyResponse> ProxyMcpRequestAsync(string podName, string jsonRpcBody, int timeoutSeconds, CancellationToken ct = default);
    Task<McpStatusResponse> GetMcpStatusAsync(string podName, CancellationToken ct = default);
    Task StopMcpProcessAsync(string podName, CancellationToken ct = default);
}
