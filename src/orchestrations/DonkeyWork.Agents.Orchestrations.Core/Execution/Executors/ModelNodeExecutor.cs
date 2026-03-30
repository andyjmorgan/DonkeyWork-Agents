using System.Text;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;
using DonkeyWork.Agents.Providers.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;

/// <summary>
/// Executor for Model nodes.
/// Renders Scriban templates, resolves credentials, and calls the model pipeline.
/// </summary>
public class ModelNodeExecutor : NodeExecutor<ModelNodeConfiguration, ModelNodeOutput>
{
    private readonly IModelPipeline _modelPipeline;
    private readonly IExternalApiKeyService _credentialService;
    private readonly IExecutionStreamWriter _streamWriter;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<ModelNodeExecutor> _logger;

    public ModelNodeExecutor(
        IModelPipeline modelPipeline,
        IExternalApiKeyService credentialService,
        IExecutionStreamWriter streamWriter,
        IExecutionContext context,
        ITemplateRenderer templateRenderer,
        ILogger<ModelNodeExecutor> logger)
        : base(streamWriter, context)
    {
        _modelPipeline = modelPipeline;
        _credentialService = credentialService;
        _streamWriter = streamWriter;
        _templateRenderer = templateRenderer;
        _logger = logger;
    }

    protected override async Task<ModelNodeOutput> ExecuteInternalAsync(
        ModelNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>();

        if (config.SystemPrompts != null && config.SystemPrompts.Count > 0)
        {
            foreach (var systemPrompt in config.SystemPrompts)
            {
                if (string.IsNullOrWhiteSpace(systemPrompt))
                    continue;

                var renderedPrompt = await _templateRenderer.RenderAsync(systemPrompt, cancellationToken);
                messages.Add(ChatMessage.FromText(ChatMessageRole.System, renderedPrompt));
            }
        }

        foreach (var userMessage in config.UserMessages)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                continue;

            var renderedMessage = await _templateRenderer.RenderAsync(userMessage, cancellationToken);
            messages.Add(ChatMessage.FromText(ChatMessageRole.User, renderedMessage));
        }

        if (messages.Count == 0 || messages.All(m => m.Role != ChatMessageRole.User))
        {
            throw new InvalidOperationException("At least one user message is required");
        }

        var credential = await _credentialService.GetByIdAsync(
            Context.UserId,
            config.CredentialId,
            cancellationToken);

        if (credential == null)
        {
            throw new InvalidOperationException(
                $"Credential not found: {config.CredentialId}");
        }

        var options = new Dictionary<string, object>();
        if (config.Temperature.HasValue)
        {
            options["temperature"] = config.Temperature.Value;
        }
        if (config.MaxOutputTokens.HasValue)
        {
            options["max_tokens"] = config.MaxOutputTokens.Value;
        }
        if (config.TopP.HasValue)
        {
            options["top_p"] = config.TopP.Value;
        }

        // Pass stream option to control whether the provider uses streaming or non-streaming API
        options["stream"] = config.Stream;

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

        var responseBuilder = new StringBuilder();
        int? totalTokens = null;
        int? inputTokens = null;
        int? outputTokens = null;

        await foreach (var evt in _modelPipeline.ExecuteAsync(request, cancellationToken))
        {
            switch (evt)
            {
                case ContentPartStartEvent contentPartStart:
                    await _streamWriter.WriteEventAsync(
                        new ContentPartStartedEvent
                        {
                            BlockIndex = contentPartStart.BlockIndex,
                            ContentType = MapContentPartType(contentPartStart.Type)
                        });
                    break;

                case ContentPartEndEvent contentPartEnd:
                    await _streamWriter.WriteEventAsync(
                        new ContentPartEndedEvent
                        {
                            BlockIndex = contentPartEnd.BlockIndex
                        });
                    break;

                case TextDeltaEvent textDelta:
                    responseBuilder.Append(textDelta.Text);

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

    private static Orchestrations.Contracts.Models.Events.ContentPartType MapContentPartType(
        DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events.ContentPartType type)
    {
        return type switch
        {
            DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events.ContentPartType.Text =>
                Orchestrations.Contracts.Models.Events.ContentPartType.Text,
            DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events.ContentPartType.Thinking =>
                Orchestrations.Contracts.Models.Events.ContentPartType.Thinking,
            DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events.ContentPartType.Image =>
                Orchestrations.Contracts.Models.Events.ContentPartType.Image,
            DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events.ContentPartType.ToolUse =>
                Orchestrations.Contracts.Models.Events.ContentPartType.ToolUse,
            DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events.ContentPartType.ToolResult =>
                Orchestrations.Contracts.Models.Events.ContentPartType.ToolResult,
            _ => Orchestrations.Contracts.Models.Events.ContentPartType.Text
        };
    }
}
