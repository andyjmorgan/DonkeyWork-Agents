using System.Diagnostics;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Core.Services;

/// <summary>
/// Scoped service for executing MCP tools with logging and auditing support.
/// </summary>
public class McpToolExecutor : IMcpToolExecutor
{
    private readonly ILogger<McpToolExecutor> _logger;
    private readonly IMcpToolDiscoveryService _toolDiscoveryService;
    private readonly IIdentityContext _identityContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpToolExecutor"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="toolDiscoveryService">The tool discovery service.</param>
    /// <param name="identityContext">The identity context for the current request.</param>
    public McpToolExecutor(
        ILogger<McpToolExecutor> logger,
        IMcpToolDiscoveryService toolDiscoveryService,
        IIdentityContext identityContext)
    {
        _logger = logger;
        _toolDiscoveryService = toolDiscoveryService;
        _identityContext = identityContext;
    }

    /// <inheritdoc />
    public async Task<CallToolResult> ExecuteAsync(
        string toolName,
        RequestContext<CallToolRequestParams> context,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdForLogging();

        _logger.LogInformation(
            "Tool invocation started: {ToolName} by user {UserId}",
            toolName,
            userId);

        var tool = _toolDiscoveryService.GetTool(toolName);
        if (tool is null)
        {
            _logger.LogWarning(
                "Tool not found: {ToolName} requested by user {UserId}",
                toolName,
                userId);

            throw new McpProtocolException(
                $"Tool '{toolName}' not found",
                McpErrorCode.InvalidParams);
        }

        // Check for destructive operations and log accordingly
        var isDestructive = tool.ProtocolTool.Annotations?.DestructiveHint == true;
        if (isDestructive)
        {
            _logger.LogWarning(
                "Destructive tool invocation: {ToolName} by user {UserId}",
                toolName,
                userId);
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await tool.InvokeAsync(context, cancellationToken);
            stopwatch.Stop();

            LogToolCompletion(toolName, userId, stopwatch.ElapsedMilliseconds, result.IsError == true);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Tool invocation failed: {ToolName} by user {UserId} after {ElapsedMs}ms",
                toolName,
                userId,
                stopwatch.ElapsedMilliseconds);

            return new CallToolResult
            {
                IsError = true,
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Tool execution failed: {ex.Message}" }
                }
            };
        }
    }

    private void LogToolCompletion(string toolName, string userId, long elapsedMs, bool isError)
    {
        if (isError)
        {
            _logger.LogWarning(
                "Tool invocation completed with error: {ToolName} by user {UserId} in {ElapsedMs}ms",
                toolName,
                userId,
                elapsedMs);
        }
        else
        {
            _logger.LogInformation(
                "Tool invocation completed: {ToolName} by user {UserId} in {ElapsedMs}ms",
                toolName,
                userId,
                elapsedMs);
        }
    }

    private string GetUserIdForLogging()
    {
        return _identityContext.IsAuthenticated
            ? _identityContext.UserId.ToString()
            : "anonymous";
    }
}
