using DonkeyWork.Agents.Mcp.Contracts.Handlers;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Core.Handlers;

/// <summary>
/// Handler for calling MCP tools with logging and auditing support.
/// </summary>
public class CallToolHandler : ICallToolHandler
{
    private readonly ILogger<CallToolHandler> _logger;
    private readonly IMcpToolExecutor _toolExecutor;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallToolHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="toolExecutor">The tool executor service.</param>
    public CallToolHandler(
        ILogger<CallToolHandler> logger,
        IMcpToolExecutor toolExecutor)
    {
        _logger = logger;
        _toolExecutor = toolExecutor;
    }

    /// <inheritdoc />
    public async ValueTask<CallToolResult> HandleAsync(
        RequestContext<CallToolRequestParams> context,
        CancellationToken cancellationToken = default)
    {
        var toolName = context.Params?.Name;

        if (string.IsNullOrEmpty(toolName))
        {
            _logger.LogWarning("Tool call request received without tool name");
            throw new McpProtocolException(
                "Tool name is required",
                McpErrorCode.InvalidParams);
        }

        return await _toolExecutor.ExecuteAsync(toolName, context, cancellationToken);
    }
}
