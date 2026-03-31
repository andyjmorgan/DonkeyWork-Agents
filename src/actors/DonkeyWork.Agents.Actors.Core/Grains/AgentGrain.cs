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
using DonkeyWork.Agents.Actors.Core.Tools;
using DonkeyWork.Agents.Actors.Core.Tools.A2a;
using DonkeyWork.Agents.Actors.Core.Tools.Mcp;
using DonkeyWork.Agents.Actors.Core.Tools.Sandbox;
using DonkeyWork.Agents.Actors.Core.Tools.Swarm;
using DonkeyWork.Agents.A2a.Contracts.Services;
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
        IA2aServerConfigurationService a2aServerConfigService,
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
            a2aServerConfigService,
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

    protected override A2aServerReference[] GetA2aServerReferences(AgentContract contract)
    {
        return contract.A2aServers;
    }

    protected override async Task InitializeA2aToolsAsync(AgentContract contract, string[] effectiveToolGroups, CancellationToken ct)
    {
        if (A2aToolProvider is not null || contract.A2aServers is not { Length: > 0 })
            return;

        var allowedIds = contract.A2aServers.Select(s => s.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allConfigs = await A2aServerConfigService.GetEnabledConnectionConfigsAsync(ct);
        var configs = allConfigs.Where(c => allowedIds.Contains(c.Id.ToString())).ToList();

        if (configs.Count == 0)
            return;

        A2aToolProvider = new A2aToolProvider();
        await A2aToolProvider.InitializeAsync(configs, Logger, ct);
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
        var (assistantMsg, _, pipelineError) = await ExecuteTurnAsync(contract, messages, async msg =>
        {
            Messages.Add(msg);
            await MessageStore.AppendMessageAsync(
                GrainContext.GrainKey, IdentityContext.UserId, msg, NextSequenceNumber++, ct);
        }, ct);

        if (pipelineError is not null)
            throw new InvalidOperationException(pipelineError);

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

    #endregion
}
