using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Contracts.Handlers;

/// <summary>
/// Handler interface for listing MCP tools.
/// </summary>
public interface IListToolsHandler
{
    /// <summary>
    /// Handles the list tools request.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list tools result containing available tools.</returns>
    ValueTask<ListToolsResult> HandleAsync(
        RequestContext<ListToolsRequestParams> context,
        CancellationToken cancellationToken = default);
}
