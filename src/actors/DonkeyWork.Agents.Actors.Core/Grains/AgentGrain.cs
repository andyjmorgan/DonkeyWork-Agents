using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Events;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Core.Middleware;
using DonkeyWork.Agents.Actors.Core.Middleware.Messages;
using DonkeyWork.Agents.Actors.Core.Options;
using DonkeyWork.Agents.Actors.Core.Providers;
using DonkeyWork.Agents.Actors.Core.Providers.Responses;
using DonkeyWork.Agents.Actors.Core.Tools;
using DonkeyWork.Agents.Actors.Core.Tools.Mcp;
using DonkeyWork.Agents.Actors.Core.Tools.ProjectManagement;
using DonkeyWork.Agents.Actors.Core.Tools.Sandbox;
using DonkeyWork.Agents.Actors.Core.Tools.Swarm;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Actors.Core.Grains;

[CollectionAgeLimit(Minutes = 25)]
public sealed class AgentGrain : Grain, IAgentGrain, IToolExecutor
{
    private readonly ILogger<AgentGrain> _logger;
    private readonly GrainContext _grainContext;
    private readonly IIdentityContext _identityContext;
    private readonly ModelPipeline _pipeline;
    private readonly AgentToolRegistry _toolRegistry;
    private readonly AnthropicOptions _anthropicOptions;
    private readonly SandboxOptions _sandboxOptions;
    private readonly IExternalApiKeyService _apiKeyService;
    private readonly IMcpServerConfigurationService _mcpServerConfigService;
    private readonly IPersistentState<AgentState> _state;

    private AgentContract? _contract;
    private CancellationTokenSource? _cts;
    private bool _explicitCancel;
    private IAgentResponseObserver? _observer;
    private McpToolProvider? _mcpToolProvider;

    private static readonly FrozenDictionary<string, Type[]> ToolGroupMap = new Dictionary<string, Type[]>
    {
        ["swarm_spawn"] = [typeof(SwarmSpawnTools)],
        ["swarm_delegate"] = [typeof(SwarmDelegateSpawnTools)],
        ["swarm_management"] = [typeof(SwarmAgentManagementTools)],
        ["project_management"] = [
            typeof(ProjectAgentTools),
            typeof(MilestoneAgentTools),
            typeof(TaskAgentTools),
            typeof(NoteAgentTools),
            typeof(ResearchAgentTools),
        ],
        ["sandbox"] = [typeof(SandboxTools)],
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public AgentGrain(
        ILogger<AgentGrain> logger,
        GrainContext grainContext,
        IIdentityContext identityContext,
        ModelPipeline pipeline,
        AgentToolRegistry toolRegistry,
        IOptions<AnthropicOptions> anthropicOptions,
        IOptions<SandboxOptions> sandboxOptions,
        IExternalApiKeyService apiKeyService,
        IMcpServerConfigurationService mcpServerConfigService,
        [PersistentState("agent", Actors.Contracts.StorageProviders.SeaweedFs)] IPersistentState<AgentState> state)
    {
        _logger = logger;
        _grainContext = grainContext;
        _identityContext = identityContext;
        _pipeline = pipeline;
        _toolRegistry = toolRegistry;
        _anthropicOptions = anthropicOptions.Value;
        _sandboxOptions = sandboxOptions.Value;
        _apiKeyService = apiKeyService;
        _mcpServerConfigService = mcpServerConfigService;
        _state = state;
    }

    #region IAgentGrain

    public async Task<AgentResult> RunAsync(AgentContract contract, string input, IAgentResponseObserver? observer)
    {
        _contract = contract;
        _observer = observer;
        _explicitCancel = false;

        SetupGrainContext(observer);

        var messages = BuildInitialMessages(contract, input);

        if (contract.PersistMessages)
        {
            _state.State.Messages.Add(new InternalContentMessage
            {
                Role = InternalMessageRole.User,
                Content = input,
            });
        }

        var timeoutSeconds = contract.TimeoutSeconds > 0 ? contract.TimeoutSeconds : 1200;
        _cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        AgentResult result = AgentResult.Empty;
        bool isError = false;

        try
        {
            result = await RunPipelineAsync(contract, messages, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            var reason = _explicitCancel ? "cancelled by user" : "timed out";
            _logger.LogWarning("Agent {Key} {Reason}", _grainContext.GrainKey, reason);
            result = AgentResult.FromText($"Agent {reason}.");
            isError = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {Key} failed", _grainContext.GrainKey);
            result = AgentResult.FromText($"Agent error: {ex.Message}");
            isError = true;
        }
        finally
        {
            await ReportToRegistryAsync(contract, result, isError);
        }

        return result;
    }

    public Task CancelAsync()
    {
        _explicitCancel = true;
        _cts?.Cancel();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<InternalMessage>> GetMessagesAsync()
    {
        return Task.FromResult<IReadOnlyList<InternalMessage>>(_state.State.Messages);
    }

    #endregion

    #region Pipeline Execution

    private async Task<AgentResult> RunPipelineAsync(
        AgentContract contract,
        List<InternalMessage> messages,
        CancellationToken ct)
    {
        var toolTypes = ResolveToolGroups(contract.ToolGroups);
        var localTools = toolTypes.Length > 0 ? _toolRegistry.GetToolDefinitions(toolTypes) : null;
        var modelId = contract.ModelId ?? _anthropicOptions.DefaultModelId;

        // Initialize MCP tools (lazy, once per activation)
        if (_mcpToolProvider is null)
        {
            var mcpConfigs = await _mcpServerConfigService.GetEnabledConnectionConfigsAsync(ct);
            if (mcpConfigs.Count > 0)
            {
                _mcpToolProvider = new McpToolProvider();
                await _mcpToolProvider.InitializeAsync(mcpConfigs, _logger, (name, success, ms, toolCount, error) =>
                {
                    Emit(new StreamMcpServerStatusEvent(_grainContext.GrainKey, name, success, ms, toolCount, error));
                }, ct);
            }
        }

        // Combine local + MCP tool definitions
        var mcpTools = _mcpToolProvider?.GetToolDefinitions() ?? [];
        IReadOnlyList<InternalToolDefinition>? tools = localTools is not null || mcpTools.Count > 0
            ? [.. (localTools ?? []), .. mcpTools]
            : null;

        var apiKey = await _apiKeyService.GetApiKeyValueAsync(ExternalApiKeyProvider.Anthropic, ct);

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("No Anthropic API key configured. Add one in Settings > API Keys.");

        // Append sandbox documentation when sandbox tools are in scope
        var hasSandbox = contract.ToolGroups.Contains("sandbox", StringComparer.OrdinalIgnoreCase);
        var systemPrompt = hasSandbox
            ? contract.SystemPrompt + SandboxTools.SystemPromptFragment
            : contract.SystemPrompt;

        var context = new ModelMiddlewareContext
        {
            Messages = messages,
            SystemPrompt = systemPrompt,
            Tools = tools,
            ProviderOptions = new ProviderOptions
            {
                ApiKey = apiKey,
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

        var assistantMsg = context.Messages.OfType<InternalAssistantMessage>().LastOrDefault();

        if (_contract?.PersistMessages == true && assistantMsg is not null)
        {
            _state.State.Messages.Add(assistantMsg);
            foreach (var toolResult in context.Messages.OfType<InternalToolResultMessage>())
            {
                _state.State.Messages.Add(toolResult);
            }
            await _state.WriteStateAsync();
        }

        return BuildAgentResult(assistantMsg);
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
            var result = await _toolRegistry.ExecuteAsync(toolName, arguments, _grainContext, _identityContext, ServiceProvider, ct, toolTypes);
            return new ToolExecutionResult(result.Content, result.IsError);
        }

        if (_mcpToolProvider?.HasTool(toolName) == true)
        {
            var result = await _mcpToolProvider.ExecuteAsync(toolName, arguments, ct);
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
                    DisplayName = _toolRegistry.GetDisplayName(toolCall.ToolName) ?? _mcpToolProvider?.GetDisplayName(toolCall.ToolName),
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
                    DisplayName = _toolRegistry.GetDisplayName(toolResponse.ToolName) ?? _mcpToolProvider?.GetDisplayName(toolResponse.ToolName),
                });
                Emit(new StreamToolCompleteEvent(
                    key, toolResponse.ToolCallId, toolResponse.ToolName,
                    toolResponse.Success, (long)toolResponse.Duration.TotalMilliseconds)
                {
                    DisplayName = _toolRegistry.GetDisplayName(toolResponse.ToolName) ?? _mcpToolProvider?.GetDisplayName(toolResponse.ToolName),
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

    #endregion

    #region Lifecycle

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Grain activated {GrainType} {GrainKey} (messages: {MessageCount}, recordExists: {RecordExists})",
            nameof(AgentGrain), this.GetPrimaryKeyString(),
            _state.State.Messages.Count, _state.RecordExists);
        return base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        if (_mcpToolProvider is not null)
        {
            await _mcpToolProvider.DisposeAsync();
            _mcpToolProvider = null;
        }

        await base.OnDeactivateAsync(reason, cancellationToken);
        sw.Stop();

        _logger.LogInformation(
            "Grain deactivated {GrainType} {GrainKey} (reason: {Reason}, cleanup: {CleanupMs}ms)",
            nameof(AgentGrain), this.GetPrimaryKeyString(),
            reason.ReasonCode, sw.ElapsedMilliseconds);
    }

    #endregion

    #region Helpers

    private void SetupGrainContext(IAgentResponseObserver? observer)
    {
        _grainContext.Observer = observer;
        _grainContext.GrainFactory = GrainFactory;
        _grainContext.Logger = _logger;
        _grainContext.UserId = _identityContext.UserId.ToString();
        _grainContext.SeaweedFsBaseUrl = _sandboxOptions.SeaweedFsBaseUrl;
        _grainContext.ProgressCallback = breadcrumb =>
            Emit(new StreamProgressEvent(_grainContext.GrainKey, breadcrumb));
    }

    private List<InternalMessage> BuildInitialMessages(AgentContract contract, string input)
    {
        var messages = new List<InternalMessage>();

        if (contract.PersistMessages && _state.State.Messages.Count > 0)
        {
            messages.AddRange(_state.State.Messages);
        }

        messages.Add(new InternalContentMessage
        {
            Role = InternalMessageRole.User,
            Content = input,
        });

        return messages;
    }

    private static AgentResult BuildAgentResult(InternalAssistantMessage? assistantMsg)
    {
        if (assistantMsg is null)
            return AgentResult.Empty;

        var parts = new List<AgentResultPart>();

        if (!string.IsNullOrEmpty(assistantMsg.TextContent))
            parts.Add(new AgentTextPart(assistantMsg.TextContent));

        foreach (var block in assistantMsg.ContentBlocks)
        {
            if (block is InternalCitationBlock citation)
                parts.Add(new AgentCitationPart(citation.Title, citation.Url, citation.CitedText));
        }

        return parts.Count > 0 ? new AgentResult(parts) : AgentResult.Empty;
    }

    private async Task ReportToRegistryAsync(AgentContract contract, AgentResult result, bool isError)
    {
        try
        {
            var conversationId = Guid.Parse(_grainContext.ConversationId);
            var registryKey = AgentKeys.Conversation(_identityContext.UserId, conversationId);
            var registry = GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);
            await registry.ReportCompletionAsync(_grainContext.GrainKey, result, isError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report completion to registry");
        }

        var reason = isError
            ? (_explicitCancel ? AgentCompleteReason.Cancelled : AgentCompleteReason.Failed)
            : AgentCompleteReason.Completed;
        Emit(new StreamAgentCompleteEvent(_grainContext.GrainKey) { Reason = reason });

        if (contract.Lifecycle == AgentLifecycle.Task)
            DeactivateOnIdle();
        else if (contract.Lifecycle == AgentLifecycle.Linger)
            DelayDeactivation(TimeSpan.FromSeconds(contract.LingerSeconds));
    }

    private static Type[] ResolveToolGroups(string[] toolGroups)
    {
        var types = new List<Type>();
        foreach (var group in toolGroups)
        {
            if (ToolGroupMap.TryGetValue(group, out var groupTypes))
                types.AddRange(groupTypes);
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
