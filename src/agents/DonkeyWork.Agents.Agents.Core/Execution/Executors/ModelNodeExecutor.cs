using System.Text;
using DonkeyWork.Agents.Agents.Contracts.Models.Events;
using DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;
using DonkeyWork.Agents.Providers.Contracts.Services;
using Microsoft.Extensions.Logging;
using Scriban;

namespace DonkeyWork.Agents.Agents.Core.Execution.Executors;

/// <summary>
/// Executor for Model nodes.
/// Renders Scriban templates, resolves credentials, and calls the model pipeline.
/// </summary>
public class ModelNodeExecutor : NodeExecutor<ModelNodeConfiguration, ModelNodeOutput>
{
    private readonly IModelPipeline _modelPipeline;
    private readonly IExternalApiKeyService _credentialService;
    private readonly IExecutionStreamWriter _streamWriter;
    private readonly ILogger<ModelNodeExecutor> _logger;

    public ModelNodeExecutor(
        IModelPipeline modelPipeline,
        IExternalApiKeyService credentialService,
        IExecutionStreamWriter streamWriter,
        IExecutionContext context,
        ILogger<ModelNodeExecutor> logger)
        : base(streamWriter, context)
    {
        _modelPipeline = modelPipeline;
        _credentialService = credentialService;
        _streamWriter = streamWriter;
        _logger = logger;
    }

    protected override async Task<ModelNodeOutput> ExecuteInternalAsync(
        ModelNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        // Prepare template variables (Pascal case with lowercase aliases)
        var templateContext = new
        {
            Input = Context.Input,
            input = Context.Input,
            Steps = Context.NodeOutputs,
            steps = Context.NodeOutputs
        };

        // Render templates
        string? systemPrompt = null;
        if (!string.IsNullOrWhiteSpace(config.SystemPrompt))
        {
            try
            {
                var systemTemplate = Template.Parse(config.SystemPrompt);
                systemPrompt = await systemTemplate.RenderAsync(templateContext);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to render system prompt template: {ex.Message}", ex);
            }
        }

        string userMessage;
        try
        {
            var userTemplate = Template.Parse(config.UserMessage);
            userMessage = await userTemplate.RenderAsync(templateContext);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to render user message template: {ex.Message}", ex);
        }

        // Resolve credential
        // Map LlmProvider to ExternalApiKeyProvider
        var credentialProvider = config.Provider switch
        {
            LlmProvider.Anthropic => ExternalApiKeyProvider.Anthropic,
            LlmProvider.OpenAI => ExternalApiKeyProvider.OpenAI,
            LlmProvider.Google => ExternalApiKeyProvider.Google,
            _ => throw new InvalidOperationException($"Unsupported provider: {config.Provider}")
        };

        var credential = await _credentialService.GetByIdAsync(
            Context.UserId,
            config.CredentialId,
            cancellationToken);

        if (credential == null)
        {
            throw new InvalidOperationException(
                $"Credential not found: {config.CredentialId}");
        }

        // Build messages list
        var messages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new ChatMessage
            {
                Role = ChatMessageRole.System,
                Content = systemPrompt
            });
        }
        messages.Add(new ChatMessage
        {
            Role = ChatMessageRole.User,
            Content = userMessage
        });

        // Build options dictionary
        var options = new Dictionary<string, object>();
        if (config.Temperature.HasValue)
        {
            options["temperature"] = config.Temperature.Value;
        }
        if (config.MaxTokens.HasValue)
        {
            options["max_tokens"] = config.MaxTokens.Value;
        }
        if (config.TopP.HasValue)
        {
            options["top_p"] = config.TopP.Value;
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
