using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Contracts.Services;

/// <summary>
/// Service for executing MCP tools with logging and auditing support.
/// </summary>
public interface IMcpToolExecutor
{
    /// <summary>
    /// Executes a tool by name.
    /// </summary>
    /// <param name="toolName">The name of the tool to execute.</param>
    /// <param name="context">The request context containing parameters and services.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the tool execution.</returns>
    Task<CallToolResult> ExecuteAsync(
        string toolName,
        RequestContext<CallToolRequestParams> context,
        CancellationToken cancellationToken = default);
}
