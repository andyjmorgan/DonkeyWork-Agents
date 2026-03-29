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
using DonkeyWork.Agents.Actors.Core.Tools.A2a;
using DonkeyWork.Agents.Actors.Core.Tools.Mcp;
using DonkeyWork.Agents.Actors.Core.Tools.Sandbox;
using DonkeyWork.Agents.Actors.Core.Tools.Swarm;
using DonkeyWork.Agents.A2a.Contracts.Services;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using DonkeyWork.Agents.Prompts.Contracts.Services;
using DonkeyWork.Agents.Providers.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Grains;

public abstract class BaseAgentGrain : Grain, IToolExecutor
{
    protected readonly ILogger Logger;
    protected new readonly GrainContext GrainContext;
    protected readonly IIdentityContext IdentityContext;
    protected readonly ModelPipeline Pipeline;
    protected readonly AgentToolRegistry ToolRegistry;
    protected readonly AnthropicOptions AnthropicOptions;
    protected readonly IExternalApiKeyService ApiKeyService;
    protected readonly IMcpServerConfigurationService McpServerConfigService;
    protected readonly IA2aServerConfigurationService A2aServerConfigService;
    protected readonly McpSandboxManagerClient McpSandboxManagerClient;
    protected readonly IGrainMessageStore MessageStore;
    protected readonly IPromptService PromptService;
    protected readonly IModelCatalogService ModelCatalogService;
    protected readonly IAgentExecutionRepository ExecutionRepository;

    protected List<InternalMessage> Messages = [];
    protected int NextSequenceNumber;
    protected AgentContract? Contract;
    protected CancellationTokenSource? Cts;
    protected bool ExplicitCancel;
    protected IAgentResponseObserver? Observer;
    private protected McpToolProvider? McpToolProvider;
    private protected A2aToolProvider? A2aToolProvider;
    protected bool HasMcpSandbox;
    protected Type[]? EffectiveToolTypes;
    protected SandboxProvisioningHandle? SandboxHandle;
    protected int ContextWindowLimit;
    protected int MaxOutputTokens;
    protected Guid ExecutionId;
    protected int TotalInputTokens;
    protected int TotalOutputTokens;
    protected DateTimeOffset ExecutionStartedAt;

    protected BaseAgentGrain(
        ILogger logger,
        GrainContext grainContext,
        IIdentityContext identityContext,
        ModelPipeline pipeline,
        AgentToolRegistry toolRegistry,
        AnthropicOptions anthropicOptions,
        IExternalApiKeyService apiKeyService,
        IMcpServerConfigurationService mcpServerConfigService,
        IA2aServerConfigurationService a2aServerConfigService,
        McpSandboxManagerClient mcpSandboxManagerClient,
        IGrainMessageStore messageStore,
        IPromptService promptService,
        IModelCatalogService modelCatalogService,
        IAgentExecutionRepository executionRepository)
    {
        Logger = logger;
        GrainContext = grainContext;
        IdentityContext = identityContext;
        Pipeline = pipeline;
        ToolRegistry = toolRegistry;
        AnthropicOptions = anthropicOptions;
        ApiKeyService = apiKeyService;
        McpServerConfigService = mcpServerConfigService;
        A2aServerConfigService = a2aServerConfigService;
        McpSandboxManagerClient = mcpSandboxManagerClient;
        MessageStore = messageStore;
        PromptService = promptService;
        ModelCatalogService = modelCatalogService;
        ExecutionRepository = executionRepository;
    }

    #region Abstract Methods

    /// <summary>
    /// Returns the MCP server references for the given contract.
    /// AgentGrain uses contract.McpServers; ConversationGrain discovers them from Navi config.
    /// </summary>
    protected abstract McpServerReference[] GetMcpServerReferences(AgentContract contract);

    /// <summary>
    /// Initializes MCP tools for this grain. Called once per activation when MCP servers are present.
    /// </summary>
    protected abstract Task InitializeMcpToolsAsync(AgentContract contract, string[] effectiveToolGroups, CancellationToken ct);

    /// <summary>
    /// Returns the A2A server references for the given contract.
    /// AgentGrain uses contract.A2aServers; ConversationGrain discovers them from Navi config.
    /// </summary>
    protected abstract A2aServerReference[] GetA2aServerReferences(AgentContract contract);

    /// <summary>
    /// Initializes A2A tools for this grain. Called once per activation when A2A servers are present.
    /// </summary>
    protected abstract Task InitializeA2aToolsAsync(AgentContract contract, string[] effectiveToolGroups, CancellationToken ct);

    /// <summary>
    /// Returns the agent catalog prompt fragment (e.g., sub-agents or Navi-connected agents).
    /// </summary>
    protected abstract Task<string?> GetAgentCatalogPromptAsync(AgentContract contract, CancellationToken ct);

    /// <summary>
    /// Returns MCP server references for deferred tools prompt, or null if not applicable.
    /// </summary>
    protected abstract McpServerReference[]? GetDeferredToolsServers(AgentContract contract);

    /// <summary>
    /// Allows subclasses to add additional swarm tool types beyond the base logic.
    /// </summary>
    protected abstract Type[] GetAdditionalSwarmToolTypes(AgentContract contract, Type[] currentTypes);

    #endregion

    #region Virtual Methods

    /// <summary>
    /// Called after messages are loaded during activation. Override to set subclass-specific state.
    /// </summary>
    protected virtual void OnMessagesLoaded(List<InternalMessage> messages)
    {
    }

    /// <summary>
    /// Called before deactivation cleanup. Override to perform subclass-specific cleanup.
    /// </summary>
    protected virtual Task OnBeforeDeactivateAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
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

        var (messages, nextSeq) = await MessageStore.LoadMessagesAsync(
            grainKey, userId, cancellationToken);

        Messages = messages;
        NextSequenceNumber = nextSeq;

        OnMessagesLoaded(messages);

        Logger.LogInformation(
            "Grain activated {GrainType} {GrainKey} (messages: {MessageCount})",
            GetType().Name, grainKey, Messages.Count);

        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        await OnBeforeDeactivateAsync(cancellationToken);

        SandboxHandle = null;

        if (McpToolProvider is not null)
        {
            await McpToolProvider.DisposeAsync();
            McpToolProvider = null;
        }

        if (A2aToolProvider is not null)
        {
            await A2aToolProvider.DisposeAsync();
            A2aToolProvider = null;
        }

        await base.OnDeactivateAsync(reason, cancellationToken);
        sw.Stop();

        Logger.LogInformation(
            "Grain deactivated {GrainType} {GrainKey} (reason: {Reason}, cleanup: {CleanupMs}ms)",
            GetType().Name, this.GetPrimaryKeyString(),
            reason.ReasonCode, sw.ElapsedMilliseconds);
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
        if (ToolRegistry.HasTool(toolName))
        {
            var result = await ToolRegistry.ExecuteAsync(toolName, arguments, GrainContext, IdentityContext, ServiceProvider, ct, EffectiveToolTypes);
            return new ToolExecutionResult(result.Content, result.IsError);
        }

        if (McpToolProvider?.HasTool(toolName) == true)
        {
            var result = await McpToolProvider.ExecuteAsync(toolName, arguments, ct);
            return new ToolExecutionResult(result.Content, result.IsError);
        }

        if (A2aToolProvider?.HasTool(toolName) == true)
        {
            var result = await A2aToolProvider.ExecuteAsync(toolName, arguments, ct);
            return new ToolExecutionResult(result.Content, result.IsError);
        }

        return new ToolExecutionResult($"Unknown tool: {toolName}", IsError: true);
    }

    #endregion

    #region Event Emission

    private protected void EmitStreamEvent(BaseMiddlewareMessage msg)
    {
        var key = GrainContext.GrainKey;

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
                    DisplayName = ToolRegistry.GetDisplayName(toolCall.ToolName) ?? McpToolProvider?.GetDisplayName(toolCall.ToolName) ?? A2aToolProvider?.GetDisplayName(toolCall.ToolName),
                });
                break;

            case ModelMiddlewareMessage { ModelMessage: ModelResponseCitationContent citation }:
                Emit(new StreamCitationEvent(key, citation.Title, citation.Url, citation.CitedText));
                break;

            case ModelMiddlewareMessage { ModelMessage: ModelResponseUsage usage }:
                TotalInputTokens += usage.InputTokens;
                TotalOutputTokens += usage.OutputTokens;
                Emit(new StreamUsageEvent(key, usage.InputTokens, usage.OutputTokens, usage.WebSearchRequests, ContextWindowLimit, MaxOutputTokens));
                break;

            case ModelMiddlewareMessage { ModelMessage: ModelResponseServerToolUse serverTool }
                when serverTool.ToolName.StartsWith("tool_search", StringComparison.OrdinalIgnoreCase):
            {
                var query = serverTool.Input?.TryGetProperty("query", out var q) == true ? q.GetString() : null;
                Emit(new StreamToolUseEvent(key, serverTool.ToolName, serverTool.ToolUseId, serverTool.Input?.GetRawText() ?? "{}")
                {
                    DisplayName = query is not null ? $"Searching tools: {query}" : "Searching tools",
                });
                break;
            }

            case ModelMiddlewareMessage { ModelMessage: ModelResponseToolSearchResult toolSearchResult }:
                Emit(new StreamToolCompleteEvent(key, toolSearchResult.ToolUseId, "tool_search", true, 0)
                {
                    DisplayName = "Tool search",
                });
                break;

            case ModelMiddlewareMessage { ModelMessage: ModelResponseCompactionContent compaction }:
                Emit(new StreamCompactionEvent(key, compaction.Summary));
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
                    DisplayName = ToolRegistry.GetDisplayName(toolResponse.ToolName) ?? McpToolProvider?.GetDisplayName(toolResponse.ToolName) ?? A2aToolProvider?.GetDisplayName(toolResponse.ToolName),
                });
                Emit(new StreamToolCompleteEvent(
                    key, toolResponse.ToolCallId, toolResponse.ToolName,
                    toolResponse.Success, (long)toolResponse.Duration.TotalMilliseconds)
                {
                    DisplayName = ToolRegistry.GetDisplayName(toolResponse.ToolName) ?? McpToolProvider?.GetDisplayName(toolResponse.ToolName) ?? A2aToolProvider?.GetDisplayName(toolResponse.ToolName),
                });
                break;

            case ErrorMessage error:
                Emit(new StreamErrorEvent(key, error.ErrorText));
                break;
        }
    }

    private protected void EmitWebSearchComplete(string key, ModelResponseWebSearchResult webSearch)
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
            Logger.LogDebug(ex, "Failed to parse web search results");
        }
    }

    #endregion

    #region Pipeline Execution

    /// <summary>
    /// Executes a single turn of the AI pipeline. Contains all shared logic for
    /// computing effective tools, initializing MCP, resolving tool configuration,
    /// building the system prompt, and running the middleware pipeline.
    /// </summary>
    /// <param name="contract">The agent contract defining model, tools, and prompts.</param>
    /// <param name="messages">The message list to pass to the pipeline context.</param>
    /// <param name="persistMessage">Callback invoked for each message the pipeline persists. TurnId is set before invocation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="turnId">Optional turn ID. If null, a new one is generated.</param>
    /// <returns>A tuple of the final assistant message (if any) and the messages list from the context.</returns>
    protected async Task<(InternalAssistantMessage? AssistantMessage, List<InternalMessage> Messages)> ExecuteTurnAsync(
        AgentContract contract,
        List<InternalMessage> messages,
        Func<InternalMessage, Task> persistMessage,
        CancellationToken ct,
        Guid? turnId = null)
    {
        // Ensure ToolGroupNames.Sandbox is in tool groups when EnableSandbox is set
        var effectiveToolGroups = contract.EnableSandbox
            && !contract.ToolGroups.Contains(ToolGroupNames.Sandbox, StringComparer.OrdinalIgnoreCase)
            ? [..contract.ToolGroups, ToolGroupNames.Sandbox]
            : contract.ToolGroups;

        Logger.LogInformation(
            "Contract: EnableSandbox={EnableSandbox}, ToolGroups=[{ToolGroups}], EffectiveToolGroups=[{EffectiveToolGroups}]",
            contract.EnableSandbox, string.Join(", ", contract.ToolGroups), string.Join(", ", effectiveToolGroups));

        SetupGrainContext();
        EnsureSandboxProvisioning(contract);

        var toolTypes = ResolveToolGroups(effectiveToolGroups);
        Logger.LogInformation("Resolved {ToolTypeCount} tool types from {GroupCount} groups",
            toolTypes.Length, effectiveToolGroups.Length);
        var modelId = contract.ModelId ?? AnthropicOptions.DefaultModelId;

        var modelDefinition = ModelCatalogService.GetModelById(modelId);
        ContextWindowLimit = modelDefinition?.MaxInputTokens ?? 0;
        MaxOutputTokens = modelDefinition?.MaxOutputTokens ?? 0;

        // Populate grain context with MCP servers, sub-agents, and tool groups for swarm tool inheritance
        GrainContext.McpServers = GetMcpServerReferences(contract);
        GrainContext.A2aServers = GetA2aServerReferences(contract);
        GrainContext.SubAgents = contract.SubAgents;
        GrainContext.ToolGroups = effectiveToolGroups;
        GrainContext.Icon = contract.Icon;
        GrainContext.DisplayName = contract.DisplayName;

        // Initialize MCP tools (lazy, once per activation)
        await InitializeMcpToolsAsync(contract, effectiveToolGroups, ct);

        // Initialize A2A tools (lazy, once per activation)
        await InitializeA2aToolsAsync(contract, effectiveToolGroups, ct);

        // Include sandbox tools if MCP servers triggered auto-sandbox
        var effectiveToolTypes = HasMcpSandbox && !effectiveToolGroups.Contains(ToolGroupNames.Sandbox, StringComparer.OrdinalIgnoreCase)
            ? [..toolTypes, typeof(SandboxTools)]
            : toolTypes;

        // Resolve agent catalog early so subclasses can lazily load data needed by GetAdditionalSwarmToolTypes
        var agentCatalog = await GetAgentCatalogPromptAsync(contract, ct);

        // Auto-include swarm tool types
        effectiveToolTypes = GetAdditionalSwarmToolTypes(contract, effectiveToolTypes);

        // Resolve tool configuration from contract
        var toolConfig = contract.ToolConfiguration;
        var hasExplicitConfig = toolConfig is not null;
        var globalDefer = toolConfig?.DeferToolLoading ?? false;

        // Build deferred types and excluded tools from contract overrides
        var deferredTypes = new HashSet<Type>();
        var excludedLocalTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in effectiveToolGroups)
        {
            if (!Tools.ToolGroupMap.Groups.TryGetValue(group, out var groupTypes))
                continue;

            // Tool groups never defer from the global setting — only MCP tools do.
            // Individual tools can still be disabled or deferred via per-tool overrides.
            if (toolConfig?.ToolOverrides is { Length: > 0 })
            {
                foreach (var ov in toolConfig.ToolOverrides)
                {
                    if (!string.Equals(ov.Source, group, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!ov.Enabled)
                    {
                        excludedLocalTools.Add(ov.ToolName);
                    }
                    else if (ov.Deferred.HasValue && ov.Deferred.Value)
                    {
                        foreach (var t in groupTypes)
                            deferredTypes.Add(t);
                    }
                }
            }
        }

        // Store effective tool types for scope checking during tool execution
        EffectiveToolTypes = effectiveToolTypes;

        var localTools = effectiveToolTypes.Length > 0
            ? ToolRegistry.GetToolDefinitions(
                effectiveToolTypes,
                deferredTypes.Count > 0 ? deferredTypes : null,
                excludedLocalTools.Count > 0 ? excludedLocalTools : null)
            : null;

        // Combine local + MCP tool definitions with per-server config
        // MCP tools default to deferred when no explicit config (backward compat)
        var mcpDefer = hasExplicitConfig ? globalDefer : true;
        IReadOnlyList<InternalToolDefinition> mcpTools;
        if (McpToolProvider is not null)
        {
            Dictionary<string, bool>? mcpPerToolDefer = null;
            HashSet<string>? excludedMcpTools = null;

            if (toolConfig?.ToolOverrides is { Length: > 0 })
            {
                foreach (var ov in toolConfig.ToolOverrides)
                {
                    if (effectiveToolGroups.Contains(ov.Source, StringComparer.OrdinalIgnoreCase))
                        continue;

                    if (!ov.Enabled)
                    {
                        excludedMcpTools ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        excludedMcpTools.Add(ov.ToolName);
                    }
                    else if (ov.Deferred.HasValue)
                    {
                        mcpPerToolDefer ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                        mcpPerToolDefer[ov.ToolName] = ov.Deferred.Value;
                    }
                }
            }

            mcpTools = McpToolProvider.GetToolDefinitions(mcpDefer, mcpPerToolDefer, excludedMcpTools);
        }
        else
        {
            mcpTools = [];
        }

        var a2aTools = A2aToolProvider?.GetToolDefinitions() ?? [];

        IReadOnlyList<InternalToolDefinition>? tools = localTools is not null || mcpTools.Count > 0 || a2aTools.Count > 0
            ? [.. (localTools ?? []), .. mcpTools, .. a2aTools]
            : null;

        var apiKey = await ApiKeyService.GetApiKeyValueAsync(ExternalApiKeyProvider.Anthropic, ct);

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("No Anthropic API key configured. Add one in Settings > API Keys.");

        // Collect all prompt parts: library prompts first, then contract system prompts
        var promptParts = new List<string>();

        foreach (var promptIdStr in contract.Prompts)
        {
            if (Guid.TryParse(promptIdStr, out var promptGuid))
            {
                var prompt = await PromptService.GetByIdAsync(promptGuid, ct);
                if (prompt is not null)
                    promptParts.Add(prompt.Content);
            }
        }

        promptParts.AddRange(contract.SystemPrompt.Where(s => !string.IsNullOrEmpty(s)));

        var combinedPrompt = string.Join("\n\n", promptParts);

        // Append sandbox documentation when sandbox tools are in scope
        var hasSandbox = contract.EnableSandbox
                         || contract.ToolGroups.Contains(ToolGroupNames.Sandbox, StringComparer.OrdinalIgnoreCase)
                         || HasMcpSandbox;
        var systemPrompt = hasSandbox
            ? combinedPrompt + SandboxTools.SystemPromptFragment
            : combinedPrompt;

        // Append agent catalog so the model knows which agents it can spawn
        if (agentCatalog is not null)
            systemPrompt += agentCatalog;

        // Append deferred MCP tools catalog so the model knows to use tool_search
        var deferredToolsServers = GetDeferredToolsServers(contract);
        systemPrompt += BuildDeferredToolsPrompt(mcpDefer, deferredToolsServers);

        var effectiveTurnId = turnId ?? Guid.NewGuid();

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
                ContextManagement = new ContextManagementOptions
                {
                    CompactionEnabled = contract.ContextManagement.CompactionEnabled
                                        && (modelDefinition?.Supports.Compaction ?? false),
                    CompactionTriggerTokens = contract.ContextManagement.CompactionTriggerTokens,
                    ClearToolResultsEnabled = contract.ContextManagement.ClearToolResultsEnabled,
                    ClearToolResultsTriggerTokens = contract.ContextManagement.ClearToolResultsTriggerTokens,
                    ClearToolResultsKeep = contract.ContextManagement.ClearToolResultsKeep,
                    ClearThinkingEnabled = contract.ContextManagement.ClearThinkingEnabled,
                    ClearThinkingKeepTurns = contract.ContextManagement.ClearThinkingKeepTurns,
                },
                Stream = contract.Stream,
            },
            ToolExecutor = this,
            CancellationToken = ct,
            TurnId = effectiveTurnId,
            PersistMessage = async msg =>
            {
                msg.TurnId = effectiveTurnId;
                await persistMessage(msg);
            },
        };

        await foreach (var msg in Pipeline.ExecuteAsync(context))
        {
            ct.ThrowIfCancellationRequested();
            EmitStreamEvent(msg);
        }

        var assistantMsg = context.Messages.OfType<InternalAssistantMessage>().LastOrDefault();
        return (assistantMsg, context.Messages);
    }

    protected static string BuildDeferredToolsPrompt(bool mcpDeferred, McpServerReference[]? mcpServers)
    {
        if (!mcpDeferred || mcpServers is not { Length: > 0 })
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("\n\n## MCP Servers");
        sb.AppendLine();
        sb.AppendLine("You have access to the following MCP servers:");
        sb.AppendLine();
        foreach (var server in mcpServers)
        {
            var desc = !string.IsNullOrEmpty(server.Description) ? server.Description : "No description";
            sb.AppendLine($"- **{server.Name}**: {desc}");
        }
        sb.AppendLine();
        sb.AppendLine("Use tool search should you need these tools.");

        return sb.ToString();
    }

    #endregion

    #region Helpers

    protected static Type[] ResolveToolGroups(string[] toolGroups)
    {
        var types = new List<Type>();
        foreach (var group in toolGroups)
        {
            if (Tools.ToolGroupMap.Groups.TryGetValue(group, out var groupTypes))
                types.AddRange(groupTypes);
        }
        return types.ToArray();
    }

    protected void Emit(StreamEventBase evt)
    {
        try
        {
            Observer?.OnEvent(evt);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to emit event {EventType}", evt.EventType);
        }
    }

    protected void SetupGrainContext()
    {
        GrainContext.Observer = Observer;
        GrainContext.GrainFactory = GrainFactory;
        GrainContext.Logger = Logger;
        GrainContext.UserId = IdentityContext.UserId.ToString();
        GrainContext.ProgressCallback = breadcrumb =>
            Emit(new StreamProgressEvent(GrainContext.GrainKey, breadcrumb));
    }

    /// <summary>
    /// Rolls back in-memory and persisted messages to the given sequence number.
    /// Used to undo partial writes when a turn is cancelled or fails.
    /// </summary>
    protected async Task RollbackStateAsync(int fromSequenceNumber)
    {
        var messagesToKeep = fromSequenceNumber;
        if (Messages.Count > messagesToKeep)
        {
            Messages.RemoveRange(messagesToKeep, Messages.Count - messagesToKeep);
        }
        NextSequenceNumber = fromSequenceNumber;

        try
        {
            await MessageStore.RollbackFromAsync(
                GrainContext.GrainKey, IdentityContext.UserId, fromSequenceNumber);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to rollback persisted messages for {Key}", GrainContext.GrainKey);
        }
    }

    protected void EnsureSandboxProvisioning(AgentContract contract)
    {
        var hasSandbox = contract.EnableSandbox
                         || contract.ToolGroups.Contains(ToolGroupNames.Sandbox, StringComparer.OrdinalIgnoreCase);
        if (!hasSandbox) return;

        // If the parent already provisioned a sandbox, reuse it directly
        if (SandboxHandle is null && !string.IsNullOrEmpty(contract.SandboxPodName))
        {
            Logger.LogInformation("Reusing parent sandbox pod {PodName}", contract.SandboxPodName);
            SandboxHandle = new SandboxProvisioningHandle();
            SandboxHandle.SetResult(contract.SandboxPodName);
            GrainContext.SandboxHandle = SandboxHandle;
            return;
        }

        if (SandboxHandle is null)
        {
            SandboxHandle = new SandboxProvisioningHandle();
            GrainContext.SandboxHandle = SandboxHandle;
            _ = ProvisionSandboxInternalAsync(SandboxHandle);
        }
        else if (SandboxHandle.Failed)
        {
            SandboxHandle.PrepareRetry();
            GrainContext.SandboxHandle = SandboxHandle;
            _ = ProvisionSandboxInternalAsync(SandboxHandle);
        }
        else
        {
            GrainContext.SandboxHandle = SandboxHandle;
        }
    }

    protected async Task ProvisionSandboxInternalAsync(SandboxProvisioningHandle handle)
    {
        var userId = IdentityContext.UserId.ToString();
        var conversationId = GrainContext.ConversationId;

        try
        {
            Emit(new StreamSandboxStatusEvent(GrainContext.GrainKey, "provisioning", "Looking for existing sandbox...", null));

            using var scope = ServiceProvider.CreateScope();

            var scopedIdentity = scope.ServiceProvider.GetService<IIdentityContext>();
            scopedIdentity?.SetIdentity(IdentityContext.UserId);

            var client = scope.ServiceProvider.GetRequiredService<SandboxManagerClient>();
            var podName = await client.FindSandboxAsync(userId, conversationId, CancellationToken.None);

            if (podName is null)
            {
                IReadOnlyList<string>? credentialDomains = null;
                var credentialMappingService = scope.ServiceProvider.GetService<ISandboxCredentialMappingService>();
                if (credentialMappingService is not null)
                {
                    credentialDomains = await credentialMappingService.GetConfiguredDomainsAsync(CancellationToken.None);
                    Logger.LogInformation(
                        "Resolved {Count} credential domain(s) for sandbox: [{Domains}]",
                        credentialDomains?.Count ?? 0,
                        credentialDomains is not null ? string.Join(", ", credentialDomains) : "none");
                }
                else
                {
                    Logger.LogWarning("ISandboxCredentialMappingService not registered - no credential domains will be injected");
                }

                Emit(new StreamSandboxStatusEvent(GrainContext.GrainKey, "provisioning", "Creating sandbox...", null));
                podName = await client.CreateSandboxAsync(userId, conversationId, progress =>
                {
                    Emit(new StreamSandboxStatusEvent(GrainContext.GrainKey, "provisioning", progress, null));
                }, CancellationToken.None, credentialDomains);
            }

            handle.SetResult(podName);
            Emit(new StreamSandboxStatusEvent(GrainContext.GrainKey, "ready", "Sandbox ready", podName));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Eager sandbox provisioning failed for {Key}", GrainContext.GrainKey);
            handle.SetFailed(ex);
            Emit(new StreamSandboxStatusEvent(GrainContext.GrainKey, "failed", ex.Message, null));
        }
    }

    #endregion
}
