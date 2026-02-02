using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Providers;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Agents.Core.Execution.Providers;

/// <summary>
/// Provider for utility node executions (message formatting, etc.).
/// </summary>
[NodeProvider]
public class UtilityNodeProvider
{
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<UtilityNodeProvider> _logger;

    public UtilityNodeProvider(
        ITemplateRenderer templateRenderer,
        ILogger<UtilityNodeProvider> logger)
    {
        _templateRenderer = templateRenderer;
        _logger = logger;
    }

    [NodeMethod(NodeType.MessageFormatter)]
    public async Task<MessageFormatterNodeOutput> ExecuteMessageFormatterAsync(
        MessageFormatterNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        var formattedMessage = await _templateRenderer.RenderAsync(config.Template, cancellationToken);

        _logger.LogDebug("Message formatter rendered template. Output length: {Length}", formattedMessage.Length);

        return new MessageFormatterNodeOutput
        {
            FormattedMessage = formattedMessage
        };
    }
}
