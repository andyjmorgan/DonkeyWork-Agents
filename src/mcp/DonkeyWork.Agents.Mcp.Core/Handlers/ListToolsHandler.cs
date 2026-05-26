using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Handlers;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Core.Handlers;

public class ListToolsHandler : IListToolsHandler
{
    private readonly ILogger<ListToolsHandler> _logger;
    private readonly IMcpToolDiscoveryService _toolDiscoveryService;
    private readonly IIdentityContext _identityContext;

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
    public ValueTask<ListToolsResult> HandleAsync(
        RequestContext<ListToolsRequestParams> context,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdForLogging();

        _logger.LogDebug("Listing tools for user {UserId}", userId);

        var tools = _toolDiscoveryService.DiscoverTools().Select(t => t.ProtocolTool).ToList();

        var result = new ListToolsResult
        {
            Tools = tools
        };

        _logger.LogInformation(
            "Returning {ToolCount} tools for user {UserId}",
            result.Tools.Count,
            userId);

        return ValueTask.FromResult(result);
    }

    private string GetUserIdForLogging()
    {
        return _identityContext.IsAuthenticated
            ? _identityContext.UserId.ToString()
            : "anonymous";
    }
}
