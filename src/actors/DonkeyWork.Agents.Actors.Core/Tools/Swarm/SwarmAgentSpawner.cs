using System.Text.Json;
using DonkeyWork.Agents.Actors.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Tools.Swarm;

public sealed class SwarmAgentSpawner
{
    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowOutOfOrderMetadataProperties = true,
    };

    private readonly IAgentExecutionRepository _executionRepository;
    private readonly ILogger<SwarmAgentSpawner> _logger;

    public SwarmAgentSpawner(
        IAgentExecutionRepository executionRepository,
        ILogger<SwarmAgentSpawner> logger)
    {
        _executionRepository = executionRepository;
        _logger = logger;
    }

    public async Task<ToolResult> SpawnAsync(
        AgentContract contract,
        string query,
        string label,
        string name,
        GrainContext context,
        IIdentityContext identityContext,
        CancellationToken ct)
    {
        var conversationId = Guid.Parse(context.ConversationId);
        var taskId = Guid.NewGuid();

        var agentKey = AgentKeys.Create(
            contract.KeyPrefix,
            identityContext.UserId,
            conversationId,
            taskId);

        // If the parent has a sandbox ready, pass the pod name so the child reuses it
        if (string.IsNullOrEmpty(contract.SandboxPodName) && context.SandboxHandle is { } handle)
        {
            if (handle.Task.IsCompletedSuccessfully)
            {
                contract = contract.WithSandboxPodName(handle.Task.Result);
            }
        }

        var contractJson = JsonSerializer.Serialize(contract, ContractJsonOptions);
        var executionId = await _executionRepository.CreateAsync(
            identityContext.UserId,
            conversationId,
            contract.AgentType,
            label,
            agentKey,
            context.GrainKey,
            contractJson,
            query,
            contract.ModelId,
            context.CurrentTurnId,
            ct);

        // Propagate caller context so the sub-agent's interceptor can hydrate
        // GrainContext and IIdentityContext without relying solely on key parsing.
        RequestContext.Set(GrainCallContextKeys.UserId, identityContext.UserId.ToString());
        RequestContext.Set(GrainCallContextKeys.ConversationId, context.ConversationId);
        if (executionId != Guid.Empty)
            RequestContext.Set(GrainCallContextKeys.ExecutionId, executionId.ToString());
        if (context.CurrentTurnId != Guid.Empty)
            RequestContext.Set(GrainCallContextKeys.ParentTurnId, context.CurrentTurnId.ToString());
        RequestContext.Set(GrainCallContextKeys.ParentGrainKey, context.GrainKey);

        var grain = context.GrainFactory.GetGrain<IAgentGrain>(agentKey);
        _ = grain.RunAsync(contract, query);

        var registryKey = AgentKeys.Conversation(identityContext.UserId, conversationId);
        var registry = context.GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);

        var timeout = contract.TimeoutSeconds > 0
            ? TimeSpan.FromSeconds(contract.TimeoutSeconds)
            : (TimeSpan?)null;

        var assignedName = await registry.RegisterAsync(agentKey, label, name, context.GrainKey, timeout);

        var response = new
        {
            agent_key = agentKey,
            agent_type = contract.AgentType,
            name = assignedName,
            label,
            status = "spawned",
            message = $"Agent '{assignedName}' has been spawned and is working on the task. Use send_message with target '{assignedName}' to communicate.",
        };

        return ToolResult.Success(JsonSerializer.Serialize(response));
    }
}
