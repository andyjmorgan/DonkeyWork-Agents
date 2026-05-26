using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Core.Services;

public class McpToolDiscoveryService : IMcpToolDiscoveryService
{
    private readonly ILogger<McpToolDiscoveryService> _logger;
    private readonly IMcpToolRegistry _toolRegistry;
    private readonly IIdentityContext _identityContext;

    public McpToolDiscoveryService(
        ILogger<McpToolDiscoveryService> logger,
        IMcpToolRegistry toolRegistry,
        IIdentityContext identityContext)
    {
        _logger = logger;
        _toolRegistry = toolRegistry;
        _identityContext = identityContext;
    }

    /// <inheritdoc />
    public IReadOnlyList<McpServerTool> DiscoverTools()
    {
        var allTools = _toolRegistry.GetAllTools();
        var filteredTools = FilterToolsForUser(allTools);

        _logger.LogDebug(
            "Discovered {ToolCount} tools for user {UserId}",
            filteredTools.Count,
            GetUserIdForLogging());

        return filteredTools;
    }

    /// <inheritdoc />
    public McpServerTool? GetTool(string name)
    {
        var tool = _toolRegistry.GetTool(name);

        if (tool is null)
        {
            return null;
        }

        if (!IsToolAccessibleToUser(tool))
        {
            _logger.LogWarning(
                "Tool {ToolName} exists but is not accessible to user {UserId}",
                name,
                GetUserIdForLogging());
            return null;
        }

        return tool;
    }

    private IReadOnlyList<McpServerTool> FilterToolsForUser(IReadOnlyList<McpServerTool> tools)
    {
        if (!_identityContext.IsAuthenticated)
        {
            _logger.LogDebug("No authenticated user, returning all tools");
            return tools;
        }

        return tools;
    }

    private bool IsToolAccessibleToUser(McpServerTool tool)
    {
        if (!_identityContext.IsAuthenticated)
        {
            return true;
        }

        return true;
    }

    private string GetUserIdForLogging()
    {
        return _identityContext.IsAuthenticated
            ? _identityContext.UserId.ToString()
            : "anonymous";
    }
}
