using System.Text.Json;
using DonkeyWork.Agents.Actors.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Events;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.Actors.Core.Middleware;
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
using DonkeyWork.Agents.Providers.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Actors.Core.Grains;

[CollectionAgeLimit(Minutes = 25)]
public sealed class AgentGrain : BaseAgentGrain, IAgentGrain
{
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
        IPromptService promptService,
        IModelCatalogService modelCatalogService,
        IAgentExecutionRepository executionRepository)
        : base(
            logger,
            grainContext,
            identityContext,
            pipeline,
            toolRegistry,
            anthropicOptions.Value,
            apiKeyService,
            mcpServerConfigService,
            mcpSandboxManagerClient,
            messageStore,
            promptService,
            modelCatalogService,
            executionRepository)
    {
    }

    #region Abstract Method Implementations

    protected override McpServerReference[] GetMcpServerReferences(AgentContract contract)
    {
        return contract.McpServers;
    }

    protected override async Task InitializeMcpToolsAsync(AgentContract contract, string[] effectiveToolGroups, CancellationToken ct)
    {
        if (McpToolProvider is not null || contract.McpServers is not { Length: > 0 })
            return;

        var allowedIds = new HashSet<Guid>(
            contract.McpServers
                .Select(s => Guid.TryParse(s.Id, out var id) ? id : (Guid?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value));

        var httpConfigs = (await McpServerConfigService.GetEnabledConnectionConfigsAsync(ct))
            .Where(c => allowedIds.Contains(c.Id))
            .ToList();
        var stdioConfigs = (await McpServerConfigService.GetEnabledStdioConfigsAsync(ct))
            .Where(c => allowedIds.Contains(c.Id))
            .ToList();

        if (httpConfigs.Count > 0 || stdioConfigs.Count > 0)
        {
            McpToolProvider = new McpToolProvider();
            await McpToolProvider.InitializeAsync(
                httpConfigs,
                stdioConfigs,
                McpSandboxManagerClient,
                IdentityContext.UserId.ToString(),
                Logger,
                (name, success, ms, toolCount, error) =>
                {
                    Emit(new StreamMcpServerStatusEvent(GrainContext.GrainKey, name, success, ms, toolCount, error));
                },
                ct);

            // Provision sandbox for MCP servers only if sandbox is already enabled on the contract
            if (contract.EnableSandbox
                && !effectiveToolGroups.Contains(ToolGroupNames.Sandbox, StringComparer.OrdinalIgnoreCase))
            {
                HasMcpSandbox = true;
                SandboxHandle = new SandboxProvisioningHandle();
                GrainContext.SandboxHandle = SandboxHandle;
                _ = ProvisionSandboxInternalAsync(SandboxHandle);
            }
        }
    }

    protected override Task<string?> GetAgentCatalogPromptAsync(AgentContract contract, CancellationToken ct)
    {
        if (contract.SubAgents is not { Length: > 0 })
            return Task.FromResult<string?>(null);

        var catalog = $"\n\n## Sub-Agents\n\nAvailable via `{ToolNames.SpawnAgent}` (use agent name):\n";
        foreach (var sa in contract.SubAgents)
        {
            var desc = !string.IsNullOrEmpty(sa.Description) ? sa.Description : "No description";
            catalog += $"- **{sa.Name}**: {desc}\n";
        }

        return Task.FromResult<string?>(catalog);
    }

    protected override McpServerReference[]? GetDeferredToolsServers(AgentContract contract)
    {
        return contract.McpServers;
    }

    protected override Type[] GetAdditionalSwarmToolTypes(AgentContract contract, Type[] currentTypes)
    {
        var result = currentTypes;

        if (contract.SubAgents is { Length: > 0 }
            && !result.Contains(typeof(SwarmAgentSpawnTools)))
        {
            result = [..result, typeof(SwarmAgentSpawnTools)];

            if (!result.Contains(typeof(SwarmAgentManagementTools)))
                result = [..result, typeof(SwarmAgentManagementTools)];
        }

        if (contract.AllowDelegation && !result.Contains(typeof(SwarmDelegateSpawnTools)))
        {
            result = [..result, typeof(SwarmDelegateSpawnTools)];

            if (!result.Contains(typeof(SwarmAgentManagementTools)))
                result = [..result, typeof(SwarmAgentManagementTools)];
        }

        return result;
    }

    #endregion

    #region IAgentGrain

    public async Task<AgentResult> RunAsync(AgentContract contract, string input, IAgentResponseObserver? observer)
    {
        Contract = contract;
        Observer = observer;
        ExplicitCancel = false;

        SetupGrainContext();

        // Read executionId set by the spawner via RequestContext
        var executionIdStr = RequestContext.Get(GrainCallContextKeys.ExecutionId) as string;
        if (Guid.TryParse(executionIdStr, out var execId))
            ExecutionId = execId;
        ExecutionStartedAt = DateTimeOffset.UtcNow;
        TotalInputTokens = 0;
        TotalOutputTokens = 0;

        var messages = BuildInitialMessages(contract, input);

        var userMsg = new InternalContentMessage
        {
            Role = InternalMessageRole.User,
            Content = input,
            Origin = MessageOrigin.User,
        };
        Messages.Add(userMsg);
        await MessageStore.AppendMessageAsync(
            GrainContext.GrainKey, IdentityContext.UserId, userMsg, NextSequenceNumber++);

        var timeoutSeconds = contract.TimeoutSeconds > 0 ? contract.TimeoutSeconds : 1200;
        Cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        AgentResult result = AgentResult.Empty;
        bool isError = false;

        try
        {
            result = await RunPipelineAsync(contract, messages, Cts.Token);
        }
        catch (OperationCanceledException)
        {
            var reason = ExplicitCancel ? "cancelled by user" : "timed out";
            Logger.LogWarning("Agent {Key} {Reason}", GrainContext.GrainKey, reason);
            result = AgentResult.FromText($"Agent {reason}.");
            isError = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Agent {Key} failed", GrainContext.GrainKey);
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
        ExplicitCancel = true;
        Cts?.Cancel();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<InternalMessage>> GetMessagesAsync()
    {
        return Task.FromResult<IReadOnlyList<InternalMessage>>(Messages);
    }

    #endregion

    #region Pipeline Execution

    private async Task<AgentResult> RunPipelineAsync(
        AgentContract contract,
        List<InternalMessage> messages,
        CancellationToken ct)
    {
        // Ensure ToolGroupNames.Sandbox is in tool groups when EnableSandbox is set (e.g. from test mode)
        var effectiveToolGroups = contract.EnableSandbox
            && !contract.ToolGroups.Contains(ToolGroupNames.Sandbox, StringComparer.OrdinalIgnoreCase)
            ? [..contract.ToolGroups, ToolGroupNames.Sandbox]
            : contract.ToolGroups;

        Logger.LogInformation(
            "Contract: EnableSandbox={EnableSandbox}, ToolGroups=[{ToolGroups}], EffectiveToolGroups=[{EffectiveToolGroups}]",
            contract.EnableSandbox, string.Join(", ", contract.ToolGroups), string.Join(", ", effectiveToolGroups));

        EnsureSandboxProvisioning(contract);

        var toolTypes = ResolveToolGroups(effectiveToolGroups);
        Logger.LogInformation("Resolved {ToolTypeCount} tool types from {GroupCount} groups",
            toolTypes.Length, effectiveToolGroups.Length);
        var modelId = contract.ModelId ?? AnthropicOptions.DefaultModelId;

        var modelDefinition = ModelCatalogService.GetModelById(modelId);
        ContextWindowLimit = modelDefinition?.MaxInputTokens ?? 0;
        MaxOutputTokens = modelDefinition?.MaxOutputTokens ?? 0;

        // Populate grain context with contract's MCP servers, sub-agents, and tool groups for swarm tool inheritance
        GrainContext.McpServers = contract.McpServers;
        GrainContext.SubAgents = contract.SubAgents;
        GrainContext.ToolGroups = effectiveToolGroups;
        GrainContext.Icon = contract.Icon;
        GrainContext.DisplayName = contract.DisplayName;

        // Initialize MCP tools (lazy, once per activation)
        await InitializeMcpToolsAsync(contract, effectiveToolGroups, ct);

        // Include sandbox tools if MCP servers triggered auto-sandbox
        var effectiveToolTypes = HasMcpSandbox && !effectiveToolGroups.Contains(ToolGroupNames.Sandbox, StringComparer.OrdinalIgnoreCase)
            ? [..toolTypes, typeof(SandboxTools)]
            : toolTypes;

        // Auto-include agent spawn tools when the contract has sub-agents configured
        Logger.LogInformation(
            "SubAgents check: Count={SubAgentCount}, Names=[{SubAgentNames}]",
            contract.SubAgents.Length,
            string.Join(", ", contract.SubAgents.Select(s => s.Name)));

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

        IReadOnlyList<InternalToolDefinition>? tools = localTools is not null || mcpTools.Count > 0
            ? [.. (localTools ?? []), .. mcpTools]
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

        // Append sub-agent catalog so the model knows which agents it can spawn
        var agentCatalog = await GetAgentCatalogPromptAsync(contract, ct);
        if (agentCatalog is not null)
            systemPrompt += agentCatalog;

        // Append deferred MCP tools catalog so the model knows to use tool_search
        systemPrompt += BuildDeferredToolsPrompt(mcpDefer, contract.McpServers);

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
            PersistMessage = async msg =>
            {
                msg.TurnId = turnId;
                Messages.Add(msg);
                await MessageStore.AppendMessageAsync(
                    GrainContext.GrainKey, IdentityContext.UserId, msg, NextSequenceNumber++, ct);
            },
        };

        await foreach (var msg in Pipeline.ExecuteAsync(context))
        {
            ct.ThrowIfCancellationRequested();
            EmitStreamEvent(msg);
        }

        var assistantMsg = context.Messages.OfType<InternalAssistantMessage>().LastOrDefault();

        return BuildAgentResult(assistantMsg);
    }

    #endregion

    #region Helpers

    private List<InternalMessage> BuildInitialMessages(AgentContract contract, string input)
    {
        var messages = new List<InternalMessage>();

        if (Messages.Count > 0)
        {
            messages.AddRange(Messages);
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
            var conversationId = Guid.Parse(GrainContext.ConversationId);
            var registryKey = AgentKeys.Conversation(IdentityContext.UserId, conversationId);
            var registry = GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);
            await registry.ReportCompletionAsync(GrainContext.GrainKey, result, isError);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to report completion to registry");
        }

        var reason = isError
            ? (ExplicitCancel ? AgentCompleteReason.Cancelled : AgentCompleteReason.Failed)
            : AgentCompleteReason.Completed;

        // Update execution audit trail
        if (ExecutionId != Guid.Empty)
        {
            var status = reason switch
            {
                AgentCompleteReason.Completed => "Completed",
                AgentCompleteReason.Cancelled => "Cancelled",
                _ => "Failed",
            };
            var durationMs = (long)(DateTimeOffset.UtcNow - ExecutionStartedAt).TotalMilliseconds;
            var outputJson = result != AgentResult.Empty
                ? JsonSerializer.Serialize(result)
                : null;
            var errorMsg = isError && !ExplicitCancel
                ? result.Parts.OfType<AgentTextPart>().FirstOrDefault()?.Text
                : null;

            await ExecutionRepository.UpdateCompletionAsync(
                ExecutionId, IdentityContext.UserId, status, outputJson, errorMsg,
                durationMs, TotalInputTokens, TotalOutputTokens);
        }

        Emit(new StreamAgentCompleteEvent(GrainContext.GrainKey) { Reason = reason });

        if (contract.Lifecycle == AgentLifecycle.Task)
            DeactivateOnIdle();
        else if (contract.Lifecycle == AgentLifecycle.Linger)
            DelayDeactivation(TimeSpan.FromSeconds(contract.LingerSeconds));
    }

    private static string BuildDeferredToolsPrompt(bool mcpDeferred, McpServerReference[]? mcpServers)
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
}
