using DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using Microsoft.Extensions.Logging;
using Scriban;

namespace DonkeyWork.Agents.Agents.Core.Execution.Executors;

/// <summary>
/// Executor for Message Formatter nodes.
/// Renders Scriban templates with access to input and previous step outputs.
/// </summary>
public class MessageFormatterNodeExecutor : NodeExecutor<MessageFormatterNodeConfiguration, MessageFormatterNodeOutput>
{
    private readonly ILogger<MessageFormatterNodeExecutor> _logger;

    public MessageFormatterNodeExecutor(
        IExecutionStreamWriter streamWriter,
        IExecutionContext context,
        ILogger<MessageFormatterNodeExecutor> logger)
        : base(streamWriter, context)
    {
        _logger = logger;
    }

    protected override async Task<MessageFormatterNodeOutput> ExecuteInternalAsync(
        MessageFormatterNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        // Prepare template variables (Pascal case with lowercase aliases)
        var templateContext = new
        {
            Input = Context.Input,
            input = Context.Input,
            Steps = Context.NodeOutputs,
            steps = Context.NodeOutputs,
            ExecutionId = Context.ExecutionId,
            executionId = Context.ExecutionId,
            UserId = Context.UserId,
            userId = Context.UserId
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
