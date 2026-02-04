using System.Runtime.CompilerServices;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;
using DonkeyWork.Agents.Providers.Contracts.Services;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Messages;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Providers.Core.Middleware;

/// <summary>
/// Model pipeline implementation that maps public API to internal middleware.
/// </summary>
public class ModelPipeline : IModelPipeline
{
    private readonly ILogger<ModelPipeline> _logger;
    private static readonly Type[] PipelineOrder =
    [
        typeof(BaseExceptionMiddleware),
        typeof(ToolMiddleware),
        typeof(GuardrailsMiddleware),
        typeof(AccumulatorMiddleware),
        typeof(ProviderMiddleware)
    ];

    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, IModelMiddleware> _middlewareCache = new();

    public ModelPipeline(IServiceProvider serviceProvider, ILogger<ModelPipeline> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async IAsyncEnumerable<ModelPipelineEvent> ExecuteAsync(
        ModelPipelineRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Map public request to internal context
        var context = MapToInternalContext(request);

        // Build and execute the internal pipeline
        var pipelineFunc = CreatePipelineFunc(PipelineOrder.ToList(), 0, cancellationToken);

        ModelResponseUsage? usage = null;

        await foreach (var message in pipelineFunc(context).WithCancellation(cancellationToken))
        {
            // Map internal messages to public events
            var evt = MapToPublicEvent(message, ref usage);
            if (evt != null)
            {
                yield return evt;
            }
        }
    }

    private static ModelMiddlewareContext MapToInternalContext(ModelPipelineRequest request)
    {
        return new ModelMiddlewareContext
        {
            Messages = request.Messages.Select(m => new InternalContentMessage
            {
                Role = MapRole(m.Role),
                Content = m.Content
            }).Cast<InternalMessage>().ToList(),
            Model = new InternalModelConfig
            {
                Provider = request.Model.Provider,
                ModelId = request.Model.ModelId,
                ApiKey = request.Model.ApiKey
            },
            ToolContext = request.Tools != null
                ? new InternalToolContext
                {
                    Tools = request.Tools.Select(t => new InternalToolDefinition
                    {
                        Name = t.Name,
                        Description = t.Description,
                        InputSchema = t.InputSchema
                    }).ToList()
                }
                : null,
            ProviderParameters = request.Options ?? new Dictionary<string, object>()
        };
    }

    private static InternalMessageRole MapRole(ChatMessageRole role)
    {
        return role switch
        {
            ChatMessageRole.System => InternalMessageRole.System,
            ChatMessageRole.User => InternalMessageRole.User,
            ChatMessageRole.Assistant => InternalMessageRole.Assistant,
            _ => InternalMessageRole.User
        };
    }

    private static ModelPipelineEvent? MapToPublicEvent(BaseMiddlewareMessage message, ref ModelResponseUsage? usage)
    {
        return message switch
        {
            ModelMiddlewareMessage { ModelMessage: ModelResponseTextContent text } =>
                new TextDeltaEvent { Text = text.Content },

            ModelMiddlewareMessage { ModelMessage: ModelResponseThinkingContent thinking } =>
                new ThinkingDeltaEvent { Text = thinking.Content, IsEncrypted = false },

            ModelMiddlewareMessage { ModelMessage: ModelResponseEncryptedThinkingContent encrypted } =>
                new ThinkingDeltaEvent { Text = encrypted.EncryptedContent, IsEncrypted = true },

            ModelMiddlewareMessage { ModelMessage: ModelResponseUsage u } =>
                CaptureUsage(u, ref usage),

            ModelMiddlewareMessage { ModelMessage: ModelResponseErrorContent error } =>
                new ErrorEvent { Message = error.ErrorMessage },

            ModelMiddlewareMessage { ModelMessage: ModelResponseStreamEnd streamEnd } =>
                new StreamEndEvent
                {
                    Reason = MapStopReason(streamEnd.Reason),
                    Usage = usage != null
                        ? new TokenUsage { InputTokens = usage.InputTokens, OutputTokens = usage.OutputTokens }
                        : null
                },

            ToolRequestMessage toolRequest =>
                new ToolCallEvent
                {
                    CallId = toolRequest.CallId,
                    ToolName = toolRequest.ToolName,
                    Arguments = toolRequest.Arguments
                },

            ToolResponseMessage toolResponse =>
                new ToolResultEvent
                {
                    CallId = toolResponse.CallId,
                    ToolName = toolResponse.ToolName,
                    Result = toolResponse.Response,
                    Success = toolResponse.Success
                },

            ModelMiddlewareMessage { ModelMessage: ModelResponseBlockStart blockStart } =>
                new ContentPartStartEvent
                {
                    BlockIndex = blockStart.BlockIndex,
                    Type = MapContentBlockType(blockStart.Type)
                },

            ModelMiddlewareMessage { ModelMessage: ModelResponseBlockEnd blockEnd } =>
                new ContentPartEndEvent
                {
                    BlockIndex = blockEnd.BlockIndex
                },

            // Ignore other message types (server tool calls, etc.)
            _ => null
        };
    }

    private static ContentPartType MapContentBlockType(InternalContentBlockType type)
    {
        return type switch
        {
            InternalContentBlockType.Text => ContentPartType.Text,
            InternalContentBlockType.Thinking => ContentPartType.Thinking,
            InternalContentBlockType.Image => ContentPartType.Image,
            InternalContentBlockType.ToolUse => ContentPartType.ToolUse,
            InternalContentBlockType.ToolResult => ContentPartType.ToolResult,
            _ => ContentPartType.Text
        };
    }

    private static ModelPipelineEvent? CaptureUsage(ModelResponseUsage u, ref ModelResponseUsage? usage)
    {
        usage = u;
        return null; // Don't emit as separate event, include in StreamEnd
    }

    private static PipelineStopReason MapStopReason(InternalStopReason reason)
    {
        return reason switch
        {
            InternalStopReason.EndTurn => PipelineStopReason.EndTurn,
            InternalStopReason.MaxTokens => PipelineStopReason.MaxTokens,
            InternalStopReason.Incomplete => PipelineStopReason.MaxTokens,
            InternalStopReason.ContentFilter => PipelineStopReason.ContentFilter,
            InternalStopReason.SafetyStop => PipelineStopReason.ContentFilter,
            InternalStopReason.Recitation => PipelineStopReason.ContentFilter,
            InternalStopReason.Cancelled => PipelineStopReason.Cancelled,
            InternalStopReason.ToolUse => PipelineStopReason.EndTurn, // Tool calls handled internally
            _ => PipelineStopReason.EndTurn
        };
    }

    private Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> CreatePipelineFunc(
        List<Type> middlewareTypes,
        int index,
        CancellationToken cancellationToken)
    {
        if (index >= middlewareTypes.Count)
        {
            return _ => EmptyAsyncEnumerableAsync<BaseMiddlewareMessage>();
        }

        var middlewareType = middlewareTypes[index];

        if (!_middlewareCache.TryGetValue(middlewareType, out var middleware))
        {
            middleware = (IModelMiddleware)_serviceProvider.GetRequiredService(middlewareType);
            _middlewareCache[middlewareType] = middleware;
        }

        var next = CreatePipelineFunc(middlewareTypes, index + 1, cancellationToken);

        return context => middleware.ExecuteAsync(context, next, cancellationToken);
    }

    private static async IAsyncEnumerable<T> EmptyAsyncEnumerableAsync<T>()
    {
        await Task.CompletedTask;
        yield break;
    }
}
