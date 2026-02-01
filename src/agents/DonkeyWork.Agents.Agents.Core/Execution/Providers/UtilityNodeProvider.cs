using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Providers;
using Microsoft.Extensions.Logging;
using Scriban;

namespace DonkeyWork.Agents.Agents.Core.Execution.Providers;

/// <summary>
/// Provider for utility node executions (message formatting, etc.).
/// </summary>
[NodeProvider]
public class UtilityNodeProvider
{
    private readonly IExecutionContext _executionContext;
    private readonly ILogger<UtilityNodeProvider> _logger;

    public UtilityNodeProvider(
        IExecutionContext executionContext,
        ILogger<UtilityNodeProvider> logger)
    {
        _executionContext = executionContext;
        _logger = logger;
    }

    [NodeMethod(NodeType.MessageFormatter)]
    public async Task<MessageFormatterNodeOutput> ExecuteMessageFormatterAsync(
        MessageFormatterNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        // Prepare template variables (Pascal case with lowercase aliases)
        var templateContext = new
        {
            Input = _executionContext.Input,
            input = _executionContext.Input,
            Steps = _executionContext.NodeOutputs,
            steps = _executionContext.NodeOutputs,
            ExecutionId = _executionContext.ExecutionId,
            executionId = _executionContext.ExecutionId,
            UserId = _executionContext.UserId,
            userId = _executionContext.UserId
        };

        // Render the template
        string formattedMessage;
        try
        {
            var template = Template.Parse(config.Template);

            if (template.HasErrors)
            {
                var errors = string.Join("; ", template.Messages.Select(m => m.Message));
                throw new InvalidOperationException($"Template parsing errors: {errors}");
            }

            formattedMessage = await template.RenderAsync(templateContext);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Failed to render message formatter template: {ex.Message}", ex);
        }

        _logger.LogDebug("Message formatter rendered template. Output length: {Length}", formattedMessage.Length);

        return new MessageFormatterNodeOutput
        {
            FormattedMessage = formattedMessage
        };
    }
}
