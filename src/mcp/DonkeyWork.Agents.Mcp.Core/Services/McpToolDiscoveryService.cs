using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Core.Services;

/// <summary>
/// Scoped service that discovers MCP tools with support for per-request filtering based on user context.
/// </summary>
public class McpToolDiscoveryService : IMcpToolDiscoveryService
{
    private readonly ILogger<McpToolDiscoveryService> _logger;
    private readonly IMcpToolRegistry _toolRegistry;
    private readonly IIdentityContext _identityContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpToolDiscoveryService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="toolRegistry">The singleton tool registry.</param>
    /// <param name="identityContext">The identity context for the current request.</param>
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

        // Check if the user has access to this tool
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
        // If not authenticated, return all tools (anonymous access)
        if (!_identityContext.IsAuthenticated)
        {
            _logger.LogDebug("No authenticated user, returning all tools");
            return tools;
        }

        // Future: Add permission-based filtering here
        // For now, return all tools for authenticated users
        return tools;
    }

    private bool IsToolAccessibleToUser(McpServerTool tool)
    {
        // If not authenticated, allow all tools (anonymous access)
        if (!_identityContext.IsAuthenticated)
        {
            return true;
        }

        // Future: Add permission-based access check here
        // For now, all authenticated users can access all tools
        return true;
    }

    private string GetUserIdForLogging()
    {
        return _identityContext.IsAuthenticated
            ? _identityContext.UserId.ToString()
            : "anonymous";
    }
}
