using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Contracts.Handlers;

/// <summary>
/// Handler interface for calling MCP tools.
/// </summary>
public interface ICallToolHandler
{
    /// <summary>
    /// Handles the call tool request.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The call tool result.</returns>
    ValueTask<CallToolResult> HandleAsync(
        RequestContext<CallToolRequestParams> context,
        CancellationToken cancellationToken = default);
}
