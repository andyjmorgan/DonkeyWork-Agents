using DonkeyWork.Agents.Mcp.Contracts.Models;

namespace DonkeyWork.Agents.Mcp.Contracts.Services;

/// <summary>
/// Service for testing MCP server connections.
/// </summary>
public interface IMcpServerTestService
{
    /// <summary>
    /// Tests connection to an MCP server and discovers available tools.
    /// </summary>
    Task<TestMcpServerResponseV1> TestConnectionAsync(Guid serverId, CancellationToken cancellationToken = default);
}
