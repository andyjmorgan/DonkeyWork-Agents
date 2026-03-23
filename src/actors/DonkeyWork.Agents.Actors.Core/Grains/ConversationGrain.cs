using System.Text.Json;
using System.Threading.Channels;
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
using DonkeyWork.Agents.Actors.Core.Tools.Mcp;
using DonkeyWork.Agents.Actors.Core.Tools.Sandbox;
using DonkeyWork.Agents.Actors.Core.Tools.Swarm;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Models;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Services;
using DonkeyWork.Agents.Conversations.Contracts.Services;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using DonkeyWork.Agents.Prompts.Contracts.Services;
using DonkeyWork.Agents.Providers.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace DonkeyWork.Agents.Actors.Core.Grains;

[CollectionAgeLimit(Minutes = 35)]
[Reentrant]
public sealed class ConversationGrain : BaseAgentGrain, IConversationGrain
{
    private readonly AgentContractRegistry _contractRegistry;
    private readonly IAgentDefinitionService _agentDefinitionService;

    private readonly Channel<ConversationMessage> _queue =
        Channel.CreateUnbounded<ConversationMessage>(new UnboundedChannelOptions { SingleReader = true });

    private Task? _processingLoop;
    private int _pendingCount;
    private CancellationTokenSource? _currentTurnCts;
    private bool _sqlRecordCreated;
    private bool _titleGenerated;
    private McpServerReference[] _discoveredMcpServers = [];
    private IReadOnlyList<NaviAgentDefinitionV1>? _naviAgentDefinitions;
    private DateTimeOffset _activatedAt;

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
        IAgentDefinitionService agentDefinitionService,
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
        _contractRegistry = contractRegistry;
        _agentDefinitionService = agentDefinitionService;
    }

    #region Abstract Method Implementations

    protected override McpServerReference[] GetMcpServerReferences(AgentContract contract)
    {
        return _discoveredMcpServers;
    }

    protected override async Task InitializeMcpToolsAsync(AgentContract contract, string[] effectiveToolGroups, CancellationToken ct)
    {
        if (McpToolProvider is not null)
            return;

        var httpConfigs = await McpServerConfigService.GetNaviConnectionConfigsAsync(ct);
        var stdioConfigs = await McpServerConfigService.GetNaviStdioConfigsAsync(ct);

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

            // Store discovered MCP servers so they persist across turns for delegate inheritance
            _discoveredMcpServers = httpConfigs.Select(c => new McpServerReference { Id = c.Id.ToString(), Name = c.Name, Description = c.Description })
                .Concat(stdioConfigs.Select(c => new McpServerReference { Id = c.Id.ToString(), Name = c.Name, Description = c.Description }))
                .ToArray();
            GrainContext.McpServers = _discoveredMcpServers;

            // Auto-include sandbox tools when MCP servers are connected
            if (!contract.ToolGroups.Contains(ToolGroupNames.Sandbox, StringComparer.OrdinalIgnoreCase))
            {
                HasMcpSandbox = true;
                SandboxHandle = new SandboxProvisioningHandle();
                GrainContext.SandboxHandle = SandboxHandle;
                _ = ProvisionSandboxInternalAsync(SandboxHandle);
            }
        }
    }

    protected override async Task<string?> GetAgentCatalogPromptAsync(AgentContract contract, CancellationToken ct)
    {
        // Discover Navi-connected custom agent definitions (lazy, once per activation)
        if (_naviAgentDefinitions is null)
        {
            _naviAgentDefinitions = await _agentDefinitionService.GetNaviConnectedAsync(ct);
        }

        if (_naviAgentDefinitions is not { Count: > 0 })
            return null;

        var catalog = $"\n\n## Custom Agents\n\nAvailable via `{ToolNames.SpawnAgent}` (use agent name):\n";
        foreach (var agent in _naviAgentDefinitions)
        {
            var desc = !string.IsNullOrEmpty(agent.Description) ? agent.Description : "No description";
            catalog += $"- **{agent.Name}** (agent_id: `{agent.Id}`): {desc}\n";
        }

        return catalog;
    }

    protected override McpServerReference[]? GetDeferredToolsServers(AgentContract contract)
    {
        return _discoveredMcpServers;
    }

    protected override Type[] GetAdditionalSwarmToolTypes(AgentContract contract, Type[] currentTypes)
    {
        var result = currentTypes;

        // Auto-include custom agent spawn tools when Navi-connected agents exist
        if (_naviAgentDefinitions is { Count: > 0 }
            && !result.Contains(typeof(SwarmAgentSpawnTools)))
        {
            result = [..result, typeof(SwarmAgentSpawnTools)];

            // Also include swarm management tools so the LLM can wait for / cancel custom agents
            if (!result.Contains(typeof(SwarmAgentManagementTools)))
                result = [..result, typeof(SwarmAgentManagementTools)];
        }

        return result;
    }

    #endregion

    #region Lifecycle Overrides

    protected override void OnMessagesLoaded(List<InternalMessage> messages)
    {
        _sqlRecordCreated = messages.Count > 0;
        _titleGenerated = messages.Count > 0;
    }

    protected override async Task OnBeforeDeactivateAsync(CancellationToken ct)
    {
        // Record execution completion for the conversation agent
        if (ExecutionId != Guid.Empty)
        {
            var grainKey = this.GetPrimaryKeyString();
            var userId = AgentKeys.ExtractUserId(grainKey);
            var durationMs = (long)(DateTimeOffset.UtcNow - _activatedAt).TotalMilliseconds;
            await ExecutionRepository.UpdateCompletionAsync(
                ExecutionId, userId, "Completed", null, null,
                durationMs, TotalInputTokens, TotalOutputTokens, ct);
        }
    }

    #endregion

    #region IConversationGrain

    public Task SubscribeAsync(IAgentResponseObserver observer)
    {
        Observer = observer;
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

        if (key == GrainContext.GrainKey)
        {
            // Self-cancel
            if (cancelScope is CancelScope.Active or CancelScope.Both)
            {
                _currentTurnCts?.Cancel();
                Logger.LogInformation("Cancelled active turn for {Key}", key);
            }

            if (cancelScope is CancelScope.Pending or CancelScope.Both)
            {
                DrainPendingMessages();
                Logger.LogInformation("Drained pending messages for {Key}", key);
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
        var registryKey = AgentKeys.Conversation(IdentityContext.UserId, Guid.Parse(GrainContext.ConversationId));
        var registry = GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);
        return await registry.ListAsync();
    }

    public Task<IReadOnlyList<InternalMessage>> GetMessagesAsync()
    {
        return Task.FromResult<IReadOnlyList<InternalMessage>>(Messages.AsReadOnly());
    }

    public async Task<IReadOnlyList<InternalMessage>> GetAgentMessagesAsync(string agentKey)
    {
        var registryKey = AgentKeys.Conversation(IdentityContext.UserId, Guid.Parse(GrainContext.ConversationId));
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
                Contract = contract;

                // Create execution record once per activation
                if (ExecutionId == Guid.Empty)
                {
                    var contractJson = System.Text.Json.JsonSerializer.Serialize(contract);
                    var conversationId = Guid.Parse(GrainContext.ConversationId);
                    ExecutionId = await ExecutionRepository.CreateAsync(
                        IdentityContext.UserId,
                        conversationId,
                        "conversation",
                        "Navi",
                        GrainContext.GrainKey,
                        null,
                        contractJson,
                        null,
                        contract.ModelId);
                    _activatedAt = DateTimeOffset.UtcNow;
                }

                var timeoutSeconds = contract.TimeoutSeconds > 0 ? contract.TimeoutSeconds : 1200;
                _currentTurnCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var ct = _currentTurnCts.Token;

                // Snapshot sequence number for rollback
                var snapshotSequenceNumber = NextSequenceNumber;

                // Add message to history
                var turnId = Guid.NewGuid();
                var internalMsg = FormatMessage(message);
                internalMsg.TurnId = turnId;
                Messages.Add(internalMsg);
                NextSequenceNumber = await MessageStore.AppendMessageAsync(
                    GrainContext.GrainKey, IdentityContext.UserId, internalMsg, NextSequenceNumber, ct);

                var preview = internalMsg is InternalContentMessage content
                    ? content.Content[..Math.Min(content.Content.Length, 100)]
                    : "Agent result";
                var source = message is UserConversationMessage ? "user" : "agent";

                if (message is UserConversationMessage)
                {
                    await EnsureSqlRecordAsync();
                }

                Emit(new StreamTurnStartEvent(GrainContext.GrainKey, source, preview));
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
                    Logger.LogWarning("Turn cancelled for {Key}", GrainContext.GrainKey);
                    await RollbackStateAsync(snapshotSequenceNumber);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Turn failed for {Key}", GrainContext.GrainKey);
                    await RollbackStateAsync(snapshotSequenceNumber);
                    Emit(new StreamErrorEvent(GrainContext.GrainKey, ex.Message));
                }
                finally
                {
                    _currentTurnCts.Dispose();
                    _currentTurnCts = null;
                }

                Emit(new StreamTurnEndEvent(GrainContext.GrainKey));
                EmitQueueStatus();
            }
        }
    }

    #endregion

    #region Pipeline Execution

    private async Task RunPipelineAsync(AgentContract contract, Guid turnId, CancellationToken ct)
    {
        var (assistantMsg, contextMessages) = await ExecuteTurnAsync(contract, Messages, async msg =>
        {
            NextSequenceNumber = await MessageStore.AppendMessageAsync(
                GrainContext.GrainKey, IdentityContext.UserId, msg, NextSequenceNumber, ct);
        }, ct, turnId);

        // Sync local list reference (pipeline may have grown it via middleware appends)
        Messages = contextMessages;

        // Emit completion with final text
        if (assistantMsg?.TextContent is not null)
        {
            Emit(new StreamCompleteEvent(GrainContext.GrainKey, assistantMsg.TextContent));
        }
    }

    #endregion

    #region Event Emission

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
            GrainContext.GrainKey, agentKey, agentType, label, text, citations, isError));
    }

    #endregion

    #region Helpers

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
        if (Messages.Count > messagesToKeep)
        {
            Messages.RemoveRange(messagesToKeep, Messages.Count - messagesToKeep);
        }
        NextSequenceNumber = fromSequenceNumber;

        // Trim persisted messages
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
            GrainContext.GrainKey,
            _pendingCount,
            _currentTurnCts is not null));
    }

    private void EmitMcpServerStatus()
    {
        if (McpToolProvider is null) return;

        foreach (var (serverName, toolCount) in McpToolProvider.GetConnectedServerSummaries())
        {
            Emit(new StreamMcpServerStatusEvent(
                GrainContext.GrainKey, serverName, true, 0, toolCount, null));
        }
    }

    private async Task EnsureSqlRecordAsync()
    {
        if (_sqlRecordCreated) return;

        using var scope = ServiceProvider.CreateScope();
        var metadataService = scope.ServiceProvider.GetRequiredService<IConversationMetadataService>();
        await metadataService.EnsureExistsAsync(
            Guid.Parse(GrainContext.ConversationId),
            IdentityContext.UserId,
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
            Guid.Parse(GrainContext.ConversationId),
            IdentityContext.UserId,
            title);

        _titleGenerated = true;
    }

    private async Task TouchTimestampAsync()
    {
        using var scope = ServiceProvider.CreateScope();
        var metadataService = scope.ServiceProvider.GetRequiredService<IConversationMetadataService>();
        await metadataService.TouchTimestampAsync(
            Guid.Parse(GrainContext.ConversationId),
            IdentityContext.UserId);
    }

    #endregion
}
