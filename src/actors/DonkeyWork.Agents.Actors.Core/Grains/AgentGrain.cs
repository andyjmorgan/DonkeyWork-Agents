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
using DonkeyWork.Agents.A2a.Contracts.Services;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using DonkeyWork.Agents.Prompts.Contracts.Services;
using DonkeyWork.Agents.Providers.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
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

    private readonly Channel<AgentMessage> _inbox = Channel.CreateUnbounded<AgentMessage>();

    protected override async Task OnBeforeDeactivateAsync(CancellationToken ct)
    {
        if (_isIdle)
        {
            if (ExecutionId != Guid.Empty)
            {
                var durationMs = (long)(DateTimeOffset.UtcNow - ExecutionStartedAt).TotalMilliseconds;
                await ExecutionRepository.UpdateCompletionAsync(
                    ExecutionId, IdentityContext.UserId, "Completed", null, null,
                    durationMs, TotalInputTokens, TotalOutputTokens, ct);
            }

            if (GrainContext.ConversationId is not null)
            {
                try
                {
                    var conversationId = Guid.Parse(GrainContext.ConversationId);
                    var registryKey = AgentKeys.Conversation(IdentityContext.UserId, conversationId);
                    var registry = GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);
                    await registry.ReportExpiredAsync(GrainContext.GrainKey);
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(ex, "Failed to report expired to registry on deactivation");
                }
            }
        }

        await base.OnBeforeDeactivateAsync(ct);
    }

    private bool _isIdle;

    #region IAgentGrain

    public async Task<AgentResult> RunAsync(AgentContract contract, string input, IAgentResponseObserver? observer)
    {
        Contract = contract;
        Observer = observer;
        ExplicitCancel = false;
        _isIdle = false;

        SetupGrainContext();
        GrainContext.MessageInbox = _inbox;

        var executionIdStr = RequestContext.Get(GrainCallContextKeys.ExecutionId) as string;
        if (Guid.TryParse(executionIdStr, out var execId))
            ExecutionId = execId;

        Guid? parentTurnId = null;
        var parentTurnIdStr = RequestContext.Get(GrainCallContextKeys.ParentTurnId) as string;
        if (Guid.TryParse(parentTurnIdStr, out var ptId))
            parentTurnId = ptId;

        var parentGrainKey = RequestContext.Get(GrainCallContextKeys.ParentGrainKey) as string;

        ExecutionStartedAt = DateTimeOffset.UtcNow;
        TotalInputTokens = 0;
        TotalOutputTokens = 0;

        var messages = BuildInitialMessages(contract, input);

        var userMsg = new InternalContentMessage
        {
            Role = InternalMessageRole.User,
            Content = input,
            Origin = MessageOrigin.User,
            AgentName = contract.DisplayName,
        };
        userMsg.ParentTurnId = parentTurnId;
        Messages.Add(userMsg);
        await MessageStore.AppendMessageAsync(
            GrainContext.GrainKey, IdentityContext.UserId, userMsg, NextSequenceNumber++);

        var timeoutSeconds = contract.TimeoutSeconds > 0 ? contract.TimeoutSeconds : 1200;
        Cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        AgentResult result = AgentResult.Empty;
        bool isError = false;

        try
        {
            result = await RunPipelineAsync(contract, messages, Cts.Token, parentTurnId);
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
        _isIdle = false;
        Cts?.Cancel();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<InternalMessage>> GetMessagesAsync()
    {
        return Task.FromResult<IReadOnlyList<InternalMessage>>(Messages);
    }

    public Task DeliverMessageAsync(AgentMessage message)
    {
        _inbox.Writer.TryWrite(message);

        if (_isIdle && Contract is not null)
        {
            _isIdle = false;
            _ = ResumeFromIdleAsync();
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Pipeline Execution

    private async Task<AgentResult> RunPipelineAsync(
        AgentContract contract,
        List<InternalMessage> messages,
        CancellationToken ct,
        Guid? parentTurnId = null)
    {
        var (assistantMsg, _, pipelineError) = await ExecuteTurnAsync(contract, messages, async msg =>
        {
            Messages.Add(msg);
            await MessageStore.AppendMessageAsync(
                GrainContext.GrainKey, IdentityContext.UserId, msg, NextSequenceNumber++, ct);
        }, ct, parentTurnId: parentTurnId, drainPendingMessages: DrainInboxAsync);

        if (pipelineError is not null)
            throw new InvalidOperationException(pipelineError);

        return BuildAgentResult(assistantMsg);
    }

    private async Task ResumeFromIdleAsync()
    {
        var contract = Contract!;
        var conversationId = Guid.Parse(GrainContext.ConversationId);
        var registryKey = AgentKeys.Conversation(IdentityContext.UserId, conversationId);

        var inboxMessages = DrainInboxMessages();
        if (inboxMessages.Count == 0)
            return;

        try
        {
            var registry = GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);
            await registry.ReportResumedAsync(GrainContext.GrainKey);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to report resumed to registry");
        }

        var messages = new List<InternalMessage>(Messages);

        foreach (var msg in inboxMessages)
        {
            Messages.Add(msg);
            await MessageStore.AppendMessageAsync(
                GrainContext.GrainKey, IdentityContext.UserId, msg, NextSequenceNumber++);
            messages.Add(msg);
        }

        var timeoutSeconds = contract.TimeoutSeconds > 0 ? contract.TimeoutSeconds : 1200;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        AgentResult resumeResult;
        try
        {
            resumeResult = await RunPipelineAsync(contract, messages, cts.Token);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Resume from idle failed for {Key}", GrainContext.GrainKey);
            Emit(new StreamErrorEvent(GrainContext.GrainKey, $"Resume failed: {ex.Message}"));
            resumeResult = AgentResult.Empty;
        }

        try
        {
            var conversationGrain = GrainFactory.GetGrain<IConversationGrain>(registryKey);
            await conversationGrain.DeliverAgentResultAsync(
                GrainContext.GrainKey,
                contract.DisplayName ?? "agent",
                resumeResult,
                false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to deliver resume result to parent");
        }

        _isIdle = true;
        DelayDeactivation(TimeSpan.FromSeconds(contract.LingerSeconds > 0 ? contract.LingerSeconds : 1800));

        try
        {
            var registry = GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);
            await registry.ReportIdleAsync(GrainContext.GrainKey, resumeResult);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to report idle after resume");
        }

        Emit(new StreamAgentIdleEvent(GrainContext.GrainKey));
    }

    #endregion

    #region Inbox

    private Task<IReadOnlyList<InternalMessage>> DrainInboxAsync()
    {
        if (Contract?.Lifecycle == AgentLifecycle.Linger)
            DelayDeactivation(TimeSpan.FromSeconds(Contract.LingerSeconds > 0 ? Contract.LingerSeconds : 1800));

        var messages = DrainInboxMessages();
        return Task.FromResult<IReadOnlyList<InternalMessage>>(messages);
    }

    private List<InternalMessage> DrainInboxMessages()
    {
        var messages = new List<InternalMessage>();

        while (_inbox.Reader.TryRead(out var agentMsg))
        {
            messages.Add(new InternalContentMessage
            {
                Role = InternalMessageRole.User,
                Content = $"<agent-message from=\"{agentMsg.FromName}\" key=\"{agentMsg.FromAgentKey}\">{agentMsg.Content}</agent-message>",
                Origin = MessageOrigin.Agent,
            });
        }

        return messages;
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
        var conversationId = Guid.Parse(GrainContext.ConversationId);
        var registryKey = AgentKeys.Conversation(IdentityContext.UserId, conversationId);
        var registry = GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);

        var reason = isError
            ? (ExplicitCancel ? AgentCompleteReason.Cancelled : AgentCompleteReason.Failed)
            : AgentCompleteReason.Completed;

        var goingIdle = !isError && contract.Lifecycle == AgentLifecycle.Linger;

        if (ExecutionId != Guid.Empty)
        {
            if (goingIdle)
            {
                await ExecutionRepository.UpdateStatusAsync(
                    ExecutionId, IdentityContext.UserId, "Idle");
            }
            else
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
        }

        if (goingIdle)
        {
            _isIdle = true;

            try
            {
                var conversationGrain = GrainFactory.GetGrain<IConversationGrain>(registryKey);
                await conversationGrain.DeliverAgentResultAsync(
                    GrainContext.GrainKey,
                    Contract?.DisplayName ?? "agent",
                    result,
                    false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to deliver result to parent before going idle");
            }

            try
            {
                await registry.ReportIdleAsync(GrainContext.GrainKey, result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to report idle to registry");
            }

            Emit(new StreamAgentIdleEvent(GrainContext.GrainKey));
            DelayDeactivation(TimeSpan.FromSeconds(contract.LingerSeconds > 0 ? contract.LingerSeconds : 1800));
            return;
        }

        try
        {
            await registry.ReportCompletionAsync(GrainContext.GrainKey, result, isError);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to report completion to registry");
        }

        Emit(new StreamAgentCompleteEvent(GrainContext.GrainKey) { Reason = reason });

        if (contract.Lifecycle == AgentLifecycle.Task)
            DeactivateOnIdle();
        else if (contract.Lifecycle == AgentLifecycle.Linger)
            DelayDeactivation(TimeSpan.FromSeconds(contract.LingerSeconds));
    }

    #endregion
}
