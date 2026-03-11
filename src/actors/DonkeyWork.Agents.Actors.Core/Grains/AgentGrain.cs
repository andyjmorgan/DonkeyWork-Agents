using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
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
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using DonkeyWork.Agents.Prompts.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IExternalApiKeyService _apiKeyService;
    private readonly IMcpServerConfigurationService _mcpServerConfigService;
    private readonly McpSandboxManagerClient _mcpSandboxManagerClient;
    private readonly IGrainMessageStore _messageStore;
    private readonly IPromptService _promptService;

    private List<InternalMessage> _messages = [];
    private int _nextSequenceNumber;
    private AgentContract? _contract;
    private CancellationTokenSource? _cts;
    private bool _explicitCancel;
    private IAgentResponseObserver? _observer;
    private McpToolProvider? _mcpToolProvider;
    private bool _hasMcpSandbox;
    private SandboxProvisioningHandle? _sandboxHandle;

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
        IExternalApiKeyService apiKeyService,
        IMcpServerConfigurationService mcpServerConfigService,
        McpSandboxManagerClient mcpSandboxManagerClient,
        IGrainMessageStore messageStore,
        IPromptService promptService)
    {
        _logger = logger;
        _grainContext = grainContext;
        _identityContext = identityContext;
        _pipeline = pipeline;
        _toolRegistry = toolRegistry;
        _anthropicOptions = anthropicOptions.Value;
        _apiKeyService = apiKeyService;
        _mcpServerConfigService = mcpServerConfigService;
        _mcpSandboxManagerClient = mcpSandboxManagerClient;
        _messageStore = messageStore;
        _promptService = promptService;
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
            var userMsg = new InternalContentMessage
            {
                Role = InternalMessageRole.User,
                Content = input,
                Origin = MessageOrigin.User,
            };
            _messages.Add(userMsg);
            await _messageStore.AppendMessageAsync(
                _grainContext.GrainKey, _identityContext.UserId, userMsg, _nextSequenceNumber++);
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
        return Task.FromResult<IReadOnlyList<InternalMessage>>(_messages);
    }

    #endregion

    #region Pipeline Execution

    private async Task<AgentResult> RunPipelineAsync(
        AgentContract contract,
        List<InternalMessage> messages,
        CancellationToken ct)
    {
        // Ensure "sandbox" is in tool groups when EnableSandbox is set (e.g. from test mode)
        var effectiveToolGroups = contract.EnableSandbox
            && !contract.ToolGroups.Contains("sandbox", StringComparer.OrdinalIgnoreCase)
            ? [..contract.ToolGroups, "sandbox"]
            : contract.ToolGroups;

        _logger.LogInformation(
            "Contract: EnableSandbox={EnableSandbox}, ToolGroups=[{ToolGroups}], EffectiveToolGroups=[{EffectiveToolGroups}]",
            contract.EnableSandbox, string.Join(", ", contract.ToolGroups), string.Join(", ", effectiveToolGroups));

        EnsureSandboxProvisioning(contract);

        var toolTypes = ResolveToolGroups(effectiveToolGroups);
        _logger.LogInformation("Resolved {ToolTypeCount} tool types from {GroupCount} groups",
            toolTypes.Length, effectiveToolGroups.Length);
        var modelId = contract.ModelId ?? _anthropicOptions.DefaultModelId;

        // Populate grain context with contract's MCP servers and sub-agents for swarm tool inheritance
        _grainContext.McpServers = contract.McpServers;
        _grainContext.SubAgents = contract.SubAgents;

        // Initialize MCP tools (lazy, once per activation)
        // Only connect to MCP servers specified in the contract's McpServers list
        if (_mcpToolProvider is null && contract.McpServers is { Length: > 0 })
        {
            var allowedIds = new HashSet<Guid>(
                contract.McpServers
                    .Select(s => Guid.TryParse(s.Id, out var id) ? id : (Guid?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value));

            var httpConfigs = (await _mcpServerConfigService.GetEnabledConnectionConfigsAsync(ct))
                .Where(c => allowedIds.Contains(c.Id))
                .ToList();
            var stdioConfigs = (await _mcpServerConfigService.GetEnabledStdioConfigsAsync(ct))
                .Where(c => allowedIds.Contains(c.Id))
                .ToList();

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

                // Auto-include sandbox tools when MCP servers are connected
                if (!effectiveToolGroups.Contains("sandbox", StringComparer.OrdinalIgnoreCase))
                {
                    _hasMcpSandbox = true;
                    _sandboxHandle = new SandboxProvisioningHandle();
                    _grainContext.SandboxHandle = _sandboxHandle;
                    _ = ProvisionSandboxInternalAsync(_sandboxHandle);
                }
            }
        }

        // Include sandbox tools if MCP servers triggered auto-sandbox
        var effectiveToolTypes = _hasMcpSandbox && !effectiveToolGroups.Contains("sandbox", StringComparer.OrdinalIgnoreCase)
            ? [..toolTypes, typeof(SandboxTools)]
            : toolTypes;
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
                         || contract.ToolGroups.Contains("sandbox", StringComparer.OrdinalIgnoreCase)
                         || _hasMcpSandbox;
        var systemPrompt = hasSandbox
            ? combinedPrompt + SandboxTools.SystemPromptFragment
            : combinedPrompt;

        var turnId = Guid.NewGuid();

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
            PersistMessage = contract.PersistMessages
                ? async msg =>
                {
                    msg.TurnId = turnId;
                    _messages.Add(msg);
                    await _messageStore.AppendMessageAsync(
                        _grainContext.GrainKey, _identityContext.UserId, msg, _nextSequenceNumber++, ct);
                }
                : null,
        };

        await foreach (var msg in _pipeline.ExecuteAsync(context))
        {
            ct.ThrowIfCancellationRequested();
            EmitStreamEvent(msg);
        }

        var assistantMsg = context.Messages.OfType<InternalAssistantMessage>().LastOrDefault();

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

        _logger.LogInformation(
            "Grain activated {GrainType} {GrainKey} (messages: {MessageCount})",
            nameof(AgentGrain), grainKey, _messages.Count);

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
        _grainContext.ProgressCallback = breadcrumb =>
            Emit(new StreamProgressEvent(_grainContext.GrainKey, breadcrumb));
    }

    private List<InternalMessage> BuildInitialMessages(AgentContract contract, string input)
    {
        var messages = new List<InternalMessage>();

        if (contract.PersistMessages && _messages.Count > 0)
        {
            messages.AddRange(_messages);
        }

        messages.Add(new InternalContentMessage
        {
            Role = InternalMessageRole.User,
            Content = input,
            Origin = MessageOrigin.User,
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

    private void EnsureSandboxProvisioning(AgentContract contract)
    {
        var hasSandbox = contract.EnableSandbox
                         || contract.ToolGroups.Contains("sandbox", StringComparer.OrdinalIgnoreCase);
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

    #endregion
}
