using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Handlers;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Core.Handlers;

/// <summary>
/// Handler for listing MCP tools with support for user-based filtering.
/// </summary>
public class ListToolsHandler : IListToolsHandler
{
    private readonly ILogger<ListToolsHandler> _logger;
    private readonly IMcpToolDiscoveryService _toolDiscoveryService;
    private readonly IIdentityContext _identityContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListToolsHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="toolDiscoveryService">The tool discovery service.</param>
    /// <param name="identityContext">The identity context for the current request.</param>
    public ListToolsHandler(
        ILogger<ListToolsHandler> logger,
        IMcpToolDiscoveryService toolDiscoveryService,
        IIdentityContext identityContext)
    {
        _logger = logger;
        _toolDiscoveryService = toolDiscoveryService;
        _identityContext = identityContext;
    }

    /// <inheritdoc />
    public async ValueTask<ListToolsResult> HandleAsync(
        RequestContext<ListToolsRequestParams> context,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdForLogging();

        _logger.LogDebug("Listing tools for user {UserId}", userId);

        var staticTools = _toolDiscoveryService.DiscoverTools().Select(t => t.ProtocolTool).ToList();
        var dynamicTools = await _toolDiscoveryService.DiscoverDynamicToolsAsync(cancellationToken);

        var result = new ListToolsResult
        {
            Tools = [..staticTools, ..dynamicTools]
        };

        _logger.LogInformation(
            "Returning {ToolCount} tools ({StaticCount} static, {DynamicCount} dynamic) for user {UserId}",
            result.Tools.Count,
            staticTools.Count,
            dynamicTools.Count,
            userId);

        return result;
    }

    private string GetUserIdForLogging()
    {
        return _identityContext.IsAuthenticated
            ? _identityContext.UserId.ToString()
            : "anonymous";
    }
}
