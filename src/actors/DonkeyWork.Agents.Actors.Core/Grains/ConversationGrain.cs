using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Events;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
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
using DonkeyWork.Agents.AgentDefinitions.Contracts.Models;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Services;
using DonkeyWork.Agents.Conversations.Contracts.Services;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using DonkeyWork.Agents.Prompts.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace DonkeyWork.Agents.Actors.Core.Grains;

[CollectionAgeLimit(Minutes = 35)]
[Reentrant]
public sealed class ConversationGrain : Grain, IConversationGrain, IToolExecutor
{
    private readonly ILogger<ConversationGrain> _logger;
    private readonly GrainContext _grainContext;
    private readonly IIdentityContext _identityContext;
    private readonly ModelPipeline _pipeline;
    private readonly AgentToolRegistry _toolRegistry;
    private readonly AgentContractRegistry _contractRegistry;
    private readonly AnthropicOptions _anthropicOptions;
    private readonly IExternalApiKeyService _apiKeyService;
    private readonly IMcpServerConfigurationService _mcpServerConfigService;
    private readonly McpSandboxManagerClient _mcpSandboxManagerClient;
    private readonly IGrainMessageStore _messageStore;
    private readonly IPromptService _promptService;
    private readonly IAgentDefinitionService _agentDefinitionService;

    private readonly Channel<ConversationMessage> _queue =
        Channel.CreateUnbounded<ConversationMessage>(new UnboundedChannelOptions { SingleReader = true });

    private IAgentResponseObserver? _observer;
    private Task? _processingLoop;
    private int _pendingCount;
    private CancellationTokenSource? _currentTurnCts;
    private List<InternalMessage> _messages = [];
    private int _nextSequenceNumber;
    private bool _sqlRecordCreated;
    private bool _titleGenerated;
    private AgentContract? _contract;
    private McpToolProvider? _mcpToolProvider;
    private bool _hasMcpSandbox;
    private SandboxProvisioningHandle? _sandboxHandle;
    private IReadOnlyList<NaviAgentDefinitionV1>? _naviAgentDefinitions;
    private Type[]? _effectiveToolTypes;
    private static readonly FrozenDictionary<string, Type[]> ToolGroupMap = new Dictionary<string, Type[]>
    {
        [ToolGroupNames.SwarmDelegate] = [typeof(SwarmDelegateSpawnTools)],
        [ToolGroupNames.SwarmManagement] = [typeof(SwarmAgentManagementTools)],
        [ToolGroupNames.ProjectManagement] = [
            typeof(ProjectAgentTools),
            typeof(MilestoneAgentTools),
            typeof(TaskAgentTools),
            typeof(NoteAgentTools),
            typeof(ResearchAgentTools),
        ],
        [ToolGroupNames.Sandbox] = [typeof(SandboxTools)],
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public ConversationGrain(
        ILogger<ConversationGrain> logger,
        GrainContext grainContext,
        ModelPipeline pipeline,
        AgentToolRegistry toolRegistry,
        AgentContractRegistry contractRegistry,
        IOptions<AnthropicOptions> anthropicOptions,
        IExternalApiKeyService apiKeyService,
        IIdentityContext identityContext,
        IMcpServerConfigurationService mcpServerConfigService,
        McpSandboxManagerClient mcpSandboxManagerClient,
        IGrainMessageStore messageStore,
        IPromptService promptService,
        IAgentDefinitionService agentDefinitionService)
    {
        _logger = logger;
        _grainContext = grainContext;
        _pipeline = pipeline;
        _toolRegistry = toolRegistry;
        _contractRegistry = contractRegistry;
        _anthropicOptions = anthropicOptions.Value;
        _apiKeyService = apiKeyService;
        _identityContext = identityContext;
        _mcpServerConfigService = mcpServerConfigService;
        _mcpSandboxManagerClient = mcpSandboxManagerClient;
        _messageStore = messageStore;
        _promptService = promptService;
        _agentDefinitionService = agentDefinitionService;
    }

    #region IConversationGrain

    public Task SubscribeAsync(IAgentResponseObserver observer)
    {
        _observer = observer;
        EnsureProcessingLoop();
        EmitQueueStatus();
        EmitMcpServerStatus();
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
        var registryKey = AgentKeys.Conversation(_identityContext.UserId, Guid.Parse(_grainContext.ConversationId));
        var registry = GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);
        return await registry.ListAsync();
    }

    public Task<IReadOnlyList<InternalMessage>> GetMessagesAsync()
    {
        return Task.FromResult<IReadOnlyList<InternalMessage>>(_messages.AsReadOnly());
    }

    public async Task<IReadOnlyList<InternalMessage>> GetAgentMessagesAsync(string agentKey)
    {
        var registryKey = AgentKeys.Conversation(_identityContext.UserId, Guid.Parse(_grainContext.ConversationId));
        var registry = GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);
        var agents = await registry.ListAsync();

        if (!agents.Any(a => a.AgentKey == agentKey))
        {
            return Array.Empty<InternalMessage>();
        }

        var agentGrain = GrainFactory.GetGrain<IAgentGrain>(agentKey);
        return await agentGrain.GetMessagesAsync();
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

                // Snapshot sequence number for rollback
                var snapshotSequenceNumber = _nextSequenceNumber;

                // Add message to history
                var turnId = Guid.NewGuid();
                var internalMsg = FormatMessage(message);
                internalMsg.TurnId = turnId;
                _messages.Add(internalMsg);
                _nextSequenceNumber = await _messageStore.AppendMessageAsync(
                    _grainContext.GrainKey, _identityContext.UserId, internalMsg, _nextSequenceNumber, ct);

                var preview = internalMsg is InternalContentMessage content
                    ? content.Content[..Math.Min(content.Content.Length, 100)]
                    : "Agent result";
                var source = message is UserConversationMessage ? "user" : "agent";

                if (message is UserConversationMessage)
                {
                    await EnsureSqlRecordAsync();
                }

                Emit(new StreamTurnStartEvent(_grainContext.GrainKey, source, preview));
                EmitQueueStatus();

                try
                {
                    await RunPipelineAsync(contract, turnId, ct);

                    if (message is UserConversationMessage userMsg)
                    {
                        await GenerateTitleAsync(userMsg.Text);
                    }
                    await TouchTimestampAsync();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Turn cancelled for {Key}", _grainContext.GrainKey);
                    await RollbackStateAsync(snapshotSequenceNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Turn failed for {Key}", _grainContext.GrainKey);
                    await RollbackStateAsync(snapshotSequenceNumber);
                    Emit(new StreamErrorEvent(_grainContext.GrainKey, ex.Message));
                }
                finally
                {
                    _currentTurnCts.Dispose();
                    _currentTurnCts = null;
                }

                Emit(new StreamTurnEndEvent(_grainContext.GrainKey));
                EmitQueueStatus();
            }
        }
    }

    #endregion

    #region Pipeline Execution

    private async Task RunPipelineAsync(AgentContract contract, Guid turnId, CancellationToken ct)
    {
        SetupGrainContext();
        EnsureSandboxProvisioning(contract);

        // Populate grain context with contract's MCP servers, sub-agents, and tool groups for swarm tool inheritance
        _grainContext.McpServers = contract.McpServers;
        _grainContext.SubAgents = contract.SubAgents;
        _grainContext.ToolGroups = contract.ToolGroups;

        var toolTypes = ResolveToolGroups(contract.ToolGroups);
        var modelId = contract.ModelId ?? _anthropicOptions.DefaultModelId;

        // Initialize MCP tools (lazy, once per activation)
        if (_mcpToolProvider is null)
        {
            var httpConfigs = await _mcpServerConfigService.GetNaviConnectionConfigsAsync(ct);
            var stdioConfigs = await _mcpServerConfigService.GetNaviStdioConfigsAsync(ct);

            if (httpConfigs.Count > 0 || stdioConfigs.Count > 0)
            {
                _mcpToolProvider = new McpToolProvider();
                await _mcpToolProvider.InitializeAsync(
                    httpConfigs,
                    stdioConfigs,
                    _mcpSandboxManagerClient,
                    _identityContext.UserId.ToString(),
                    _logger,
                    (name, success, ms, toolCount, error) =>
                    {
                        Emit(new StreamMcpServerStatusEvent(_grainContext.GrainKey, name, success, ms, toolCount, error));
                    },
                    ct);

                // Populate grain context with actual connected MCP servers so delegates can inherit them
                _grainContext.McpServers = httpConfigs.Select(c => new McpServerReference { Id = c.Id.ToString(), Name = c.Name })
                    .Concat(stdioConfigs.Select(c => new McpServerReference { Id = c.Id.ToString(), Name = c.Name }))
                    .ToArray();

                // Auto-include sandbox tools when MCP servers are connected
                if (!contract.ToolGroups.Contains(ToolGroupNames.Sandbox, StringComparer.OrdinalIgnoreCase))
                {
                    _hasMcpSandbox = true;
                    _sandboxHandle = new SandboxProvisioningHandle();
                    _grainContext.SandboxHandle = _sandboxHandle;
                    _ = ProvisionSandboxInternalAsync(_sandboxHandle);
                }
            }
        }

        // Discover Navi-connected custom agent definitions (lazy, once per activation)
        if (_naviAgentDefinitions is null)
        {
            _naviAgentDefinitions = await _agentDefinitionService.GetNaviConnectedAsync(ct);
        }

        // Include sandbox tools if MCP servers triggered auto-sandbox
        var effectiveToolTypes = _hasMcpSandbox && !contract.ToolGroups.Contains(ToolGroupNames.Sandbox, StringComparer.OrdinalIgnoreCase)
            ? [..toolTypes, typeof(SandboxTools)]
            : toolTypes;

        // Auto-include custom agent spawn tools when Navi-connected agents exist
        if (_naviAgentDefinitions.Count > 0
            && !effectiveToolTypes.Contains(typeof(SwarmAgentSpawnTools)))
        {
            effectiveToolTypes = [..effectiveToolTypes, typeof(SwarmAgentSpawnTools)];

            // Also include swarm management tools so the LLM can wait for / cancel custom agents
            if (!effectiveToolTypes.Contains(typeof(SwarmAgentManagementTools)))
                effectiveToolTypes = [..effectiveToolTypes, typeof(SwarmAgentManagementTools)];
        }

        // Store effective tool types for execution scope (includes dynamically added tools)
        _effectiveToolTypes = effectiveToolTypes;

        var localTools = effectiveToolTypes.Length > 0 ? _toolRegistry.GetToolDefinitions(effectiveToolTypes) : null;

        // Combine local + MCP tool definitions
        var mcpTools = _mcpToolProvider?.GetToolDefinitions() ?? [];
        IReadOnlyList<InternalToolDefinition>? tools = localTools is not null || mcpTools.Count > 0
            ? [.. (localTools ?? []), .. mcpTools]
            : null;

        var apiKey = await _apiKeyService.GetApiKeyValueAsync(ExternalApiKeyProvider.Anthropic, ct);

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("No Anthropic API key configured. Add one in Settings > API Keys.");

        // Collect all prompt parts: library prompts first, then contract system prompts
        var promptParts = new List<string>();

        foreach (var promptIdStr in contract.Prompts)
        {
            if (Guid.TryParse(promptIdStr, out var promptGuid))
            {
                var prompt = await _promptService.GetByIdAsync(promptGuid, ct);
                if (prompt is not null)
                    promptParts.Add(prompt.Content);
            }
        }

        promptParts.AddRange(contract.SystemPrompt.Where(s => !string.IsNullOrEmpty(s)));

        var combinedPrompt = string.Join("\n\n", promptParts);

        // Append sandbox documentation when sandbox tools are in scope
        var hasSandbox = contract.EnableSandbox
                         || contract.ToolGroups.Contains(ToolGroupNames.Sandbox, StringComparer.OrdinalIgnoreCase)
                         || _hasMcpSandbox;
        var systemPrompt = hasSandbox
            ? combinedPrompt + SandboxTools.SystemPromptFragment
            : combinedPrompt;

        // Append custom agent catalog when Navi-connected agents are available
        if (_naviAgentDefinitions is { Count: > 0 })
        {
            var catalog = $"\n\n## Custom Agents\n\nAvailable via `{ToolNames.SpawnAgent}` (use agent name):\n";
            foreach (var agent in _naviAgentDefinitions)
            {
                var desc = !string.IsNullOrEmpty(agent.Description) ? agent.Description : "No description";
                catalog += $"- **{agent.Name}** (agent_id: `{agent.Id}`): {desc}\n";
            }
            systemPrompt += catalog;
        }

        var context = new ModelMiddlewareContext
        {
            Messages = _messages,
            SystemPrompt = systemPrompt,
            Tools = tools,
            ProviderOptions = new ProviderOptions
            {
                ApiKey = apiKey,
                ModelId = modelId,
                MaxTokens = contract.MaxTokens,
                ThinkingBudgetTokens = contract.ReasoningEffort is null
                    ? (contract.ThinkingBudgetTokens > 0 ? contract.ThinkingBudgetTokens : null)
                    : null,
                ReasoningEffort = contract.ReasoningEffort,
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
            TurnId = turnId,
            PersistMessage = async msg =>
            {
                msg.TurnId = turnId;
                _nextSequenceNumber = await _messageStore.AppendMessageAsync(
                    _grainContext.GrainKey, _identityContext.UserId, msg, _nextSequenceNumber, ct);
            },
        };

        await foreach (var msg in _pipeline.ExecuteAsync(context))
        {
            ct.ThrowIfCancellationRequested();
            EmitStreamEvent(msg);
        }

        // Sync local list reference (pipeline may have grown it via middleware appends)
        _messages = context.Messages;

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
            var result = await _toolRegistry.ExecuteAsync(toolName, arguments, _grainContext, _identityContext, ServiceProvider, ct, _effectiveToolTypes);
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
            _ when agentKey.StartsWith(AgentKeys.DelegatePrefix) => AgentTypes.Delegate,
            _ when agentKey.StartsWith(AgentKeys.AgentPrefix) => AgentTypes.Agent,
            _ => "unknown",
        };

        Emit(new StreamAgentResultDataEvent(
            _grainContext.GrainKey, agentKey, agentType, label, text, citations, isError));
    }

    #endregion

    #region Lifecycle

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Extract userId from the grain key because IIdentityContext is not yet
        // hydrated during activation (the GrainContextInterceptor only runs on
        // incoming grain calls, which happen after activation).
        var grainKey = this.GetPrimaryKeyString();
        var userId = AgentKeys.ExtractUserId(grainKey);

        var (messages, nextSeq) = await _messageStore.LoadMessagesAsync(
            grainKey, userId, cancellationToken);

        _messages = messages;
        _nextSequenceNumber = nextSeq;
        _sqlRecordCreated = _messages.Count > 0;
        _titleGenerated = _messages.Count > 0;

        _logger.LogInformation(
            "Grain activated {GrainType} {GrainKey} (messages: {MessageCount})",
            nameof(ConversationGrain), grainKey, _messages.Count);

        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        _sandboxHandle = null;

        if (_mcpToolProvider is not null)
        {
            await _mcpToolProvider.DisposeAsync();
            _mcpToolProvider = null;
        }

        await base.OnDeactivateAsync(reason, cancellationToken);
        sw.Stop();

        _logger.LogInformation(
            "Grain deactivated {GrainType} {GrainKey} (reason: {Reason}, cleanup: {CleanupMs}ms)",
            nameof(ConversationGrain), this.GetPrimaryKeyString(),
            reason.ReasonCode, sw.ElapsedMilliseconds);
    }

    #endregion

    #region Helpers

    private void SetupGrainContext()
    {
        _grainContext.Observer = _observer;
        _grainContext.GrainFactory = GrainFactory;
        _grainContext.Logger = _logger;
        _grainContext.UserId = _identityContext.UserId.ToString();
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
                Origin = MessageOrigin.User,
            },
            AgentResultConversationMessage agentResult => new InternalContentMessage
            {
                Role = InternalMessageRole.User,
                Content = FormatAgentResult(agentResult),
                Origin = MessageOrigin.Agent,
                AgentName = agentResult.Label,
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
            return $"<agent-notification>\nAgent '{msg.Label}' (key: {msg.AgentKey}) FAILED:\n{detail}\n</agent-notification>";
        }

        return $"<agent-notification>\nAgent '{msg.Label}' (key: {msg.AgentKey}) has completed successfully.\n</agent-notification>";
    }

    private async Task RollbackStateAsync(int fromSequenceNumber)
    {
        // Trim in-memory messages
        var messagesToKeep = fromSequenceNumber;
        if (_messages.Count > messagesToKeep)
        {
            _messages.RemoveRange(messagesToKeep, _messages.Count - messagesToKeep);
        }
        _nextSequenceNumber = fromSequenceNumber;

        // Trim persisted messages
        try
        {
            await _messageStore.RollbackFromAsync(
                _grainContext.GrainKey, _identityContext.UserId, fromSequenceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback persisted messages for {Key}", _grainContext.GrainKey);
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

    private void EmitMcpServerStatus()
    {
        if (_mcpToolProvider is null) return;

        foreach (var (serverName, toolCount) in _mcpToolProvider.GetConnectedServerSummaries())
        {
            Emit(new StreamMcpServerStatusEvent(
                _grainContext.GrainKey, serverName, true, 0, toolCount, null));
        }
    }

    private void EnsureSandboxProvisioning(AgentContract contract)
    {
        var hasSandbox = contract.EnableSandbox
                         || contract.ToolGroups.Contains(ToolGroupNames.Sandbox, StringComparer.OrdinalIgnoreCase);
        if (!hasSandbox) return;

        if (_sandboxHandle is null)
        {
            _sandboxHandle = new SandboxProvisioningHandle();
            _grainContext.SandboxHandle = _sandboxHandle;
            _ = ProvisionSandboxInternalAsync(_sandboxHandle);
        }
        else if (_sandboxHandle.Failed)
        {
            _sandboxHandle.PrepareRetry();
            _grainContext.SandboxHandle = _sandboxHandle;
            _ = ProvisionSandboxInternalAsync(_sandboxHandle);
        }
        else
        {
            _grainContext.SandboxHandle = _sandboxHandle;
        }
    }

    private async Task ProvisionSandboxInternalAsync(SandboxProvisioningHandle handle)
    {
        var userId = _identityContext.UserId.ToString();
        var conversationId = _grainContext.ConversationId;

        try
        {
            Emit(new StreamSandboxStatusEvent(_grainContext.GrainKey, "provisioning", "Looking for existing sandbox...", null));

            using var scope = ServiceProvider.CreateScope();

            // Hydrate the scoped IIdentityContext so DbContext query filters work
            var scopedIdentity = scope.ServiceProvider.GetService<IIdentityContext>();
            scopedIdentity?.SetIdentity(_identityContext.UserId);

            var client = scope.ServiceProvider.GetRequiredService<SandboxManagerClient>();

            var podName = await client.FindSandboxAsync(userId, conversationId, CancellationToken.None);

            if (podName is null)
            {
                // Resolve dynamic credential domains for the user
                IReadOnlyList<string>? credentialDomains = null;
                var credentialMappingService = scope.ServiceProvider.GetService<ISandboxCredentialMappingService>();
                if (credentialMappingService is not null)
                {
                    credentialDomains = await credentialMappingService.GetConfiguredDomainsAsync(CancellationToken.None);
                    _logger.LogInformation(
                        "Resolved {Count} credential domain(s) for sandbox: [{Domains}]",
                        credentialDomains?.Count ?? 0,
                        credentialDomains is not null ? string.Join(", ", credentialDomains) : "none");
                }
                else
                {
                    _logger.LogWarning("ISandboxCredentialMappingService not registered - no credential domains will be injected");
                }

                Emit(new StreamSandboxStatusEvent(_grainContext.GrainKey, "provisioning", "Creating sandbox...", null));
                podName = await client.CreateSandboxAsync(userId, conversationId, progress =>
                {
                    Emit(new StreamSandboxStatusEvent(_grainContext.GrainKey, "provisioning", progress, null));
                }, CancellationToken.None, credentialDomains);
            }

            handle.SetResult(podName);
            Emit(new StreamSandboxStatusEvent(_grainContext.GrainKey, "ready", "Sandbox ready", podName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eager sandbox provisioning failed for {Key}", _grainContext.GrainKey);
            handle.SetFailed(ex);
            Emit(new StreamSandboxStatusEvent(_grainContext.GrainKey, "failed", ex.Message, null));
        }
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

    private async Task EnsureSqlRecordAsync()
    {
        if (_sqlRecordCreated) return;

        using var scope = ServiceProvider.CreateScope();
        var metadataService = scope.ServiceProvider.GetRequiredService<IConversationMetadataService>();
        await metadataService.EnsureExistsAsync(
            Guid.Parse(_grainContext.ConversationId),
            _identityContext.UserId,
            "New conversation");

        _sqlRecordCreated = true;
    }

    private async Task GenerateTitleAsync(string firstUserMessage)
    {
        if (_titleGenerated) return;

        var title = firstUserMessage.Length > 50 ? firstUserMessage[..50] + "..." : firstUserMessage;

        using var scope = ServiceProvider.CreateScope();
        var metadataService = scope.ServiceProvider.GetRequiredService<IConversationMetadataService>();
        await metadataService.UpdateTitleAsync(
            Guid.Parse(_grainContext.ConversationId),
            _identityContext.UserId,
            title);

        _titleGenerated = true;
    }

    private async Task TouchTimestampAsync()
    {
        using var scope = ServiceProvider.CreateScope();
        var metadataService = scope.ServiceProvider.GetRequiredService<IConversationMetadataService>();
        await metadataService.TouchTimestampAsync(
            Guid.Parse(_grainContext.ConversationId),
            _identityContext.UserId);
    }

    #endregion
}
