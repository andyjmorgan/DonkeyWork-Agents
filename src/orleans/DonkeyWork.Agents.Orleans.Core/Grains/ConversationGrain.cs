using System.Collections.Frozen;
using System.Text.Json;
using System.Threading.Channels;
using DonkeyWork.Agents.Orleans.Contracts.Contracts;
using DonkeyWork.Agents.Orleans.Contracts.Events;
using DonkeyWork.Agents.Orleans.Contracts.Grains;
using DonkeyWork.Agents.Orleans.Contracts.Messages;
using DonkeyWork.Agents.Orleans.Contracts.Models;
using DonkeyWork.Agents.Orleans.Core.Middleware;
using DonkeyWork.Agents.Orleans.Core.Middleware.Messages;
using DonkeyWork.Agents.Orleans.Core.Options;
using DonkeyWork.Agents.Orleans.Core.Providers;
using DonkeyWork.Agents.Orleans.Core.Providers.Responses;
using DonkeyWork.Agents.Orleans.Core.Tools;
using DonkeyWork.Agents.Orleans.Core.Tools.Swarm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace DonkeyWork.Agents.Orleans.Core.Grains;

[CollectionAgeLimit(Minutes = 35)]
[Reentrant]
public sealed class ConversationGrain : Grain, IConversationGrain, IToolExecutor
{
    private readonly ILogger<ConversationGrain> _logger;
    private readonly GrainContext _grainContext;
    private readonly ModelPipeline _pipeline;
    private readonly AgentToolRegistry _toolRegistry;
    private readonly AgentContractRegistry _contractRegistry;
    private readonly AnthropicOptions _anthropicOptions;
    private readonly IPersistentState<AgentState> _state;

    private readonly Channel<ConversationMessage> _queue =
        Channel.CreateUnbounded<ConversationMessage>(new UnboundedChannelOptions { SingleReader = true });

    private IAgentResponseObserver? _observer;
    private Task? _processingLoop;
    private int _pendingCount;
    private CancellationTokenSource? _currentTurnCts;
    private List<InternalMessage>? _stateSnapshot;
    private AgentContract? _contract;

    private static readonly FrozenDictionary<string, Type> ToolGroupMap = new Dictionary<string, Type>
    {
        ["swarm_spawn"] = typeof(SwarmSpawnTools),
        ["swarm_delegate"] = typeof(SwarmDelegateSpawnTools),
        ["swarm_management"] = typeof(SwarmAgentManagementTools),
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public ConversationGrain(
        ILogger<ConversationGrain> logger,
        GrainContext grainContext,
        ModelPipeline pipeline,
        AgentToolRegistry toolRegistry,
        AgentContractRegistry contractRegistry,
        IOptions<AnthropicOptions> anthropicOptions,
        [PersistentState("conversation", "AgentStore")] IPersistentState<AgentState> state)
    {
        _logger = logger;
        _grainContext = grainContext;
        _pipeline = pipeline;
        _toolRegistry = toolRegistry;
        _contractRegistry = contractRegistry;
        _anthropicOptions = anthropicOptions.Value;
        _state = state;
    }

    #region IConversationGrain

    public Task SubscribeAsync(IAgentResponseObserver observer)
    {
        _observer = observer;
        EnsureProcessingLoop();
        EmitQueueStatus();
        return Task.CompletedTask;
    }

    public Task PostUserMessageAsync(string message)
    {
        var msg = new UserConversationMessage(message, DateTimeOffset.UtcNow);
        _queue.Writer.TryWrite(msg);
        Interlocked.Increment(ref _pendingCount);
        EnsureProcessingLoop();
        EmitQueueStatus();
        return Task.CompletedTask;
    }

    public Task DeliverAgentResultAsync(string agentKey, string label, AgentResult? result, bool isError)
    {
        var msg = new AgentResultConversationMessage(agentKey, label, result, isError, DateTimeOffset.UtcNow);
        _queue.Writer.TryWrite(msg);
        Interlocked.Increment(ref _pendingCount);

        // Emit result data event for the frontend
        EmitAgentResultData(agentKey, label, result, isError);
        EnsureProcessingLoop();
        EmitQueueStatus();
        return Task.CompletedTask;
    }

    public Task CancelByKeyAsync(string key, string? scope = null)
    {
        var cancelScope = Enum.TryParse<CancelScope>(scope, true, out var parsed) ? parsed : CancelScope.Active;

        if (key == _grainContext.GrainKey)
        {
            // Self-cancel
            if (cancelScope is CancelScope.Active or CancelScope.Both)
            {
                _currentTurnCts?.Cancel();
                _logger.LogInformation("Cancelled active turn for {Key}", key);
            }

            if (cancelScope is CancelScope.Pending or CancelScope.Both)
            {
                DrainPendingMessages();
                _logger.LogInformation("Drained pending messages for {Key}", key);
            }

            Emit(new StreamCancelledEvent(key, cancelScope.ToString()));
        }
        else
        {
            // Cancel a sub-agent
            var agentGrain = GrainFactory.GetGrain<IAgentGrain>(key);
            _ = agentGrain.CancelAsync();
        }

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<TrackedAgent>> ListAgentsAsync()
    {
        var registryKey = AgentKeys.Conversation(_grainContext.UserId, Guid.Parse(_grainContext.ConversationId));
        var registry = GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);
        return await registry.ListAsync();
    }

    #endregion

    #region Processing Loop

    private void EnsureProcessingLoop()
    {
        if (_processingLoop is null || _processingLoop.IsCompleted)
        {
            _processingLoop = ProcessQueueAsync();
        }
    }

    private async Task ProcessQueueAsync()
    {
        var reader = _queue.Reader;

        while (await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out var message))
            {
                Interlocked.Decrement(ref _pendingCount);

                var contract = ResolveContract();
                _contract = contract;

                var timeoutSeconds = contract.TimeoutSeconds > 0 ? contract.TimeoutSeconds : 1200;
                _currentTurnCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var ct = _currentTurnCts.Token;

                // Snapshot state for rollback
                _stateSnapshot = new List<InternalMessage>(_state.State.Messages);

                // Add message to history
                var internalMsg = FormatMessage(message);
                _state.State.Messages.Add(internalMsg);

                var preview = internalMsg is InternalContentMessage content
                    ? content.Content[..Math.Min(content.Content.Length, 100)]
                    : "Agent result";
                var source = message is UserConversationMessage ? "user" : "agent";

                Emit(new StreamTurnStartEvent(_grainContext.GrainKey, source, preview));
                EmitQueueStatus();

                try
                {
                    await RunPipelineAsync(contract, ct);
                    await _state.WriteStateAsync();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Turn cancelled for {Key}", _grainContext.GrainKey);
                    RollbackState();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Turn failed for {Key}", _grainContext.GrainKey);
                    RollbackState();
                    Emit(new StreamErrorEvent(_grainContext.GrainKey, ex.Message));
                }
                finally
                {
                    _currentTurnCts.Dispose();
                    _currentTurnCts = null;
                    _stateSnapshot = null;
                }

                Emit(new StreamTurnEndEvent(_grainContext.GrainKey));
                EmitQueueStatus();
            }
        }
    }

    #endregion

    #region Pipeline Execution

    private async Task RunPipelineAsync(AgentContract contract, CancellationToken ct)
    {
        SetupGrainContext();

        var toolTypes = ResolveToolGroups(contract.ToolGroups);
        var tools = toolTypes.Length > 0 ? _toolRegistry.GetToolDefinitions(toolTypes) : null;
        var modelId = contract.ModelId ?? _anthropicOptions.DefaultModelId;

        var context = new ModelMiddlewareContext
        {
            Messages = _state.State.Messages,
            SystemPrompt = contract.SystemPrompt,
            Tools = tools,
            ProviderOptions = new ProviderOptions
            {
                ApiKey = _anthropicOptions.ApiKey,
                ModelId = modelId,
                MaxTokens = contract.MaxTokens,
                ThinkingBudgetTokens = contract.ThinkingBudgetTokens > 0 ? contract.ThinkingBudgetTokens : null,
                WebSearch = new WebSearchOptions
                {
                    Enabled = contract.WebSearch.Enabled,
                    MaxUses = contract.WebSearch.MaxUses > 0 ? contract.WebSearch.MaxUses : null,
                },
                WebFetch = new WebFetchOptions
                {
                    Enabled = contract.WebFetch.Enabled,
                    MaxUses = contract.WebFetch.MaxUses > 0 ? contract.WebFetch.MaxUses : null,
                },
                Stream = contract.Stream,
            },
            ToolExecutor = this,
            CancellationToken = ct,
        };

        await foreach (var msg in _pipeline.ExecuteAsync(context))
        {
            ct.ThrowIfCancellationRequested();
            EmitStreamEvent(msg);
        }

        // Copy accumulated messages back to state (the pipeline may have added assistant/tool-result messages)
        _state.State.Messages = context.Messages;

        // Emit completion with final text
        var assistantMsg = context.Messages.OfType<InternalAssistantMessage>().LastOrDefault();
        if (assistantMsg?.TextContent is not null)
        {
            Emit(new StreamCompleteEvent(_grainContext.GrainKey, assistantMsg.TextContent));
        }
    }

    #endregion

    #region IToolExecutor

    Task<ToolExecutionResult> IToolExecutor.ExecuteAsync(string toolName, JsonElement arguments, CancellationToken ct)
    {
        return ExecuteToolInternalAsync(toolName, arguments, ct);
    }

    private async Task<ToolExecutionResult> ExecuteToolInternalAsync(
        string toolName, JsonElement arguments, CancellationToken ct)
    {
        if (_toolRegistry.HasTool(toolName))
        {
            var toolTypes = _contract is not null ? ResolveToolGroups(_contract.ToolGroups) : null;
            var result = await _toolRegistry.ExecuteAsync(toolName, arguments, _grainContext, ct, toolTypes);
            return new ToolExecutionResult(result.Content, result.IsError);
        }

        return new ToolExecutionResult($"Unknown tool: {toolName}", IsError: true);
    }

    #endregion

    #region Event Emission

    private void EmitStreamEvent(BaseMiddlewareMessage msg)
    {
        var key = _grainContext.GrainKey;

        switch (msg)
        {
            case ModelMiddlewareMessage { ModelMessage: ModelResponseTextContent text }:
                Emit(new StreamMessageEvent(key, text.Content));
                break;

            case ModelMiddlewareMessage { ModelMessage: ModelResponseThinkingContent thinking }
                when !string.IsNullOrEmpty(thinking.Content):
                Emit(new StreamThinkingEvent(key, thinking.Content));
                break;

            case ModelMiddlewareMessage { ModelMessage: ModelResponseToolCall toolCall }:
                Emit(new StreamToolUseEvent(key, toolCall.ToolName, toolCall.ToolUseId, toolCall.Input.GetRawText())
                {
                    DisplayName = _toolRegistry.GetDisplayName(toolCall.ToolName),
                });
                break;

            case ModelMiddlewareMessage { ModelMessage: ModelResponseCitationContent citation }:
                Emit(new StreamCitationEvent(key, citation.Title, citation.Url, citation.CitedText));
                break;

            case ModelMiddlewareMessage { ModelMessage: ModelResponseUsage usage }:
                Emit(new StreamUsageEvent(key, usage.InputTokens, usage.OutputTokens, usage.WebSearchRequests));
                break;

            case ModelMiddlewareMessage { ModelMessage: ModelResponseWebSearchResult webSearch }:
                Emit(new StreamWebSearchEvent(key, webSearch.ToolUseId));
                EmitWebSearchComplete(key, webSearch);
                break;

            case ToolResponseMessage toolResponse:
                Emit(new StreamToolResultEvent(
                    key, toolResponse.ToolCallId, toolResponse.ToolName,
                    toolResponse.Response, toolResponse.Success, (long)toolResponse.Duration.TotalMilliseconds)
                {
                    DisplayName = _toolRegistry.GetDisplayName(toolResponse.ToolName),
                });
                Emit(new StreamToolCompleteEvent(
                    key, toolResponse.ToolCallId, toolResponse.ToolName,
                    toolResponse.Success, (long)toolResponse.Duration.TotalMilliseconds)
                {
                    DisplayName = _toolRegistry.GetDisplayName(toolResponse.ToolName),
                });
                break;

            case ErrorMessage error:
                Emit(new StreamErrorEvent(key, error.ErrorText));
                break;
        }
    }

    private void EmitWebSearchComplete(string key, ModelResponseWebSearchResult webSearch)
    {
        try
        {
            var entries = new List<WebSearchResultEntry>();
            var doc = JsonDocument.Parse(webSearch.RawJson);
            if (doc.RootElement.TryGetProperty("results", out var results) &&
                results.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in results.EnumerateArray())
                {
                    var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                    var url = item.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
                    entries.Add(new WebSearchResultEntry(title, url));
                }
            }
            Emit(new StreamWebSearchCompleteEvent(key, webSearch.ToolUseId, entries));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse web search results");
        }
    }

    private void EmitAgentResultData(string agentKey, string label, AgentResult? result, bool isError)
    {
        string? text = null;
        List<StreamAgentCitation>? citations = null;

        if (result is not null)
        {
            var textParts = result.Parts.OfType<AgentTextPart>().Select(p => p.Text);
            text = string.Join("\n", textParts);

            var citationParts = result.Parts.OfType<AgentCitationPart>().ToList();
            if (citationParts.Count > 0)
            {
                citations = citationParts
                    .Select(c => new StreamAgentCitation(c.Title, c.Url, c.CitedText))
                    .ToList();
            }
        }

        // Determine agent type from the key prefix
        var agentType = agentKey switch
        {
            _ when agentKey.StartsWith(AgentKeys.ResearchPrefix) => "research",
            _ when agentKey.StartsWith(AgentKeys.DeepResearchPrefix) => "deep_research",
            _ when agentKey.StartsWith(AgentKeys.DelegatePrefix) => "delegate",
            _ => "unknown",
        };

        Emit(new StreamAgentResultDataEvent(
            _grainContext.GrainKey, agentKey, agentType, label, text, citations, isError));
    }

    #endregion

    #region Helpers

    private void SetupGrainContext()
    {
        _grainContext.Observer = _observer;
        _grainContext.GrainFactory = GrainFactory;
        _grainContext.Logger = _logger;
        _grainContext.ProgressCallback = breadcrumb =>
            Emit(new StreamProgressEvent(_grainContext.GrainKey, breadcrumb));
    }

    private AgentContract ResolveContract()
    {
        var descriptor = _contractRegistry.GetContract("conversation");
        return descriptor?.Contract ?? AgentContracts.Conversation();
    }

    private static InternalMessage FormatMessage(ConversationMessage message)
    {
        return message switch
        {
            UserConversationMessage user => new InternalContentMessage
            {
                Role = InternalMessageRole.User,
                Content = user.Text,
            },
            AgentResultConversationMessage agentResult => new InternalContentMessage
            {
                Role = InternalMessageRole.User,
                Content = FormatAgentResult(agentResult),
            },
            _ => new InternalContentMessage
            {
                Role = InternalMessageRole.User,
                Content = message.ToString() ?? string.Empty,
            },
        };
    }

    private static string FormatAgentResult(AgentResultConversationMessage msg)
    {
        if (msg.IsError)
        {
            var detail = msg.Result is not null
                ? string.Join("\n", msg.Result.Parts.OfType<AgentTextPart>().Select(p => p.Text))
                : "No details available";
            return $"[Agent Notification] Agent '{msg.Label}' (key: {msg.AgentKey}) FAILED:\n{detail}";
        }

        return $"[Agent Notification] Agent '{msg.Label}' (key: {msg.AgentKey}) has completed successfully. " +
               $"Use the wait_for_agent tool with agent_key=\"{msg.AgentKey}\" to retrieve the full results.";
    }

    private void RollbackState()
    {
        if (_stateSnapshot is not null)
        {
            _state.State.Messages = _stateSnapshot;
        }
    }

    private void DrainPendingMessages()
    {
        while (_queue.Reader.TryRead(out _))
        {
            Interlocked.Decrement(ref _pendingCount);
        }
    }

    private void EmitQueueStatus()
    {
        Emit(new StreamQueueStatusEvent(
            _grainContext.GrainKey,
            _pendingCount,
            _currentTurnCts is not null));
    }

    private static Type[] ResolveToolGroups(string[] toolGroups)
    {
        var types = new List<Type>();
        foreach (var group in toolGroups)
        {
            if (ToolGroupMap.TryGetValue(group, out var type))
                types.Add(type);
        }
        return types.ToArray();
    }

    private void Emit(StreamEventBase evt)
    {
        try
        {
            _observer?.OnEvent(evt);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to emit event {EventType}", evt.EventType);
        }
    }

    #endregion
}
