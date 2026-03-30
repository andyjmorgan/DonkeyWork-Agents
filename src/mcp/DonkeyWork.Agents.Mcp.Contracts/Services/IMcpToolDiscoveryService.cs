using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Contracts.Services;

/// <summary>
/// Service for discovering MCP tools from assemblies.
/// Scoped service that supports per-request filtering based on user context.
/// </summary>
public interface IMcpToolDiscoveryService
{
    /// <summary>
    /// Discovers all available tools for the current user context.
    /// </summary>
    /// <returns>A read-only collection of discovered tools.</returns>
    IReadOnlyList<McpServerTool> DiscoverTools();

    /// <summary>
    /// Discovers dynamic tools from external sources (e.g., A2A agents).
    /// </summary>
    Task<IReadOnlyList<Tool>> DiscoverDynamicToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tool by name.
    /// </summary>
    /// <param name="name">The name of the tool to retrieve.</param>
    /// <returns>The tool if found; otherwise, null.</returns>
    McpServerTool? GetTool(string name);
}
