using System.Text;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;
using DonkeyWork.Agents.Providers.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;

/// <summary>
/// Executor for Multimodal Chat Model nodes.
/// Renders Scriban templates, resolves credentials, and calls the model pipeline.
/// </summary>
public class MultimodalChatNodeExecutor : NodeExecutor<MultimodalChatModelNodeConfiguration, ModelNodeOutput>
{
    private readonly IModelPipeline _modelPipeline;
    private readonly IExternalApiKeyService _credentialService;
    private readonly IExecutionStreamWriter _streamWriter;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<MultimodalChatNodeExecutor> _logger;

    public MultimodalChatNodeExecutor(
        IModelPipeline modelPipeline,
        IExternalApiKeyService credentialService,
        IExecutionStreamWriter streamWriter,
        IExecutionContext context,
        ITemplateRenderer templateRenderer,
        ILogger<MultimodalChatNodeExecutor> logger)
        : base(streamWriter, context)
    {
        _modelPipeline = modelPipeline;
        _credentialService = credentialService;
        _streamWriter = streamWriter;
        _templateRenderer = templateRenderer;
        _logger = logger;
    }

    protected override async Task<ModelNodeOutput> ExecuteInternalAsync(
        MultimodalChatModelNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        // Build messages list
        var messages = new List<ChatMessage>();

        // Render system prompts (if any)
        if (config.SystemPrompts != null && config.SystemPrompts.Count > 0)
        {
            foreach (var systemPrompt in config.SystemPrompts)
            {
                if (string.IsNullOrWhiteSpace(systemPrompt))
                    continue;

                var renderedPrompt = await _templateRenderer.RenderAsync(systemPrompt, cancellationToken);
                messages.Add(new ChatMessage
                {
                    Role = ChatMessageRole.System,
                    Content = renderedPrompt
                });
            }
        }

        // Render user messages
        foreach (var userMessage in config.UserMessages)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                continue;

            var renderedMessage = await _templateRenderer.RenderAsync(userMessage, cancellationToken);
            messages.Add(new ChatMessage
            {
                Role = ChatMessageRole.User,
                Content = renderedMessage
            });
        }

        if (messages.Count == 0 || messages.All(m => m.Role != ChatMessageRole.User))
        {
            throw new InvalidOperationException("At least one user message is required");
        }

        // Resolve credential
        var credential = await _credentialService.GetByIdAsync(
            Context.UserId,
            config.CredentialId,
            cancellationToken);

        if (credential == null)
        {
            throw new InvalidOperationException(
                $"Credential not found: {config.CredentialId}");
        }

        // Build options dictionary
        var options = new Dictionary<string, object>();
        if (config.Temperature.HasValue)
        {
            options["temperature"] = config.Temperature.Value;
        }
        if (config.MaxOutputTokens.HasValue)
        {
            options["max_tokens"] = config.MaxOutputTokens.Value;
        }

        // Create pipeline request
        var request = new ModelPipelineRequest
        {
            Messages = messages,
            Model = new PipelineModelConfig
            {
                Provider = config.Provider,
                ModelId = config.ModelId,
                ApiKey = credential.Fields[CredentialFieldType.ApiKey]
            },
            Options = options.Count > 0 ? options : null
        };

        // Execute pipeline and stream events
        var responseBuilder = new StringBuilder();
        int? totalTokens = null;
        int? inputTokens = null;
        int? outputTokens = null;

        await foreach (var evt in _modelPipeline.ExecuteAsync(request, cancellationToken))
        {
            switch (evt)
            {
                case TextDeltaEvent textDelta:
                    responseBuilder.Append(textDelta.Text);

                    // Emit TokenDelta event to execution stream
                    await _streamWriter.WriteEventAsync(
                        new TokenDeltaEvent
                        {
                            Delta = textDelta.Text
                        });
                    break;

                case StreamEndEvent streamEnd:
                    if (streamEnd.Usage != null)
                    {
                        totalTokens = streamEnd.Usage.TotalTokens;
                        inputTokens = streamEnd.Usage.InputTokens;
                        outputTokens = streamEnd.Usage.OutputTokens;
                    }
                    break;

                case ErrorEvent errorEvent:
                    throw new InvalidOperationException(
                        $"Model pipeline error: {errorEvent.Message}");
            }
        }

        var responseText = responseBuilder.ToString();

        return new ModelNodeOutput
        {
            ResponseText = responseText,
            TotalTokens = totalTokens,
            InputTokens = inputTokens,
            OutputTokens = outputTokens
        };
    }
}
