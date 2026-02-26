using System.Text.Json;
using DonkeyWork.Agents.Orleans.Contracts.Contracts;
using DonkeyWork.Agents.Orleans.Contracts.Events;
using DonkeyWork.Agents.Orleans.Contracts.Grains;
using DonkeyWork.Agents.Orleans.Contracts.Models;

namespace DonkeyWork.Agents.Orleans.Core.Tools.Swarm;

internal static class SwarmAgentSpawner
{
    public static async Task<ToolResult> SpawnAsync(
        AgentContract contract,
        string query,
        string label,
        GrainContext context,
        CancellationToken ct)
    {
        var conversationId = Guid.Parse(context.ConversationId);
        var taskId = Guid.NewGuid();

        var agentKey = AgentKeys.Create(
            contract.KeyPrefix,
            context.UserId,
            conversationId,
            taskId);

        // Notify observer of the spawn
        context.Observer?.OnEvent(new StreamAgentSpawnEvent(
            context.GrainKey,
            agentKey,
            contract.AgentType)
        {
            Label = label,
        });

        // Get the agent grain and fire-and-forget the execution
        var grain = context.GrainFactory.GetGrain<IAgentGrain>(agentKey);
        _ = grain.RunAsync(contract, query, context.Observer);

        // Register with the conversation's agent registry
        var registryKey = AgentKeys.Conversation(context.UserId, conversationId);
        var registry = context.GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);

        var timeout = contract.TimeoutSeconds > 0
            ? TimeSpan.FromSeconds(contract.TimeoutSeconds)
            : (TimeSpan?)null;

        await registry.RegisterAsync(agentKey, label, context.GrainKey, timeout);

        var response = new
        {
            agent_key = agentKey,
            agent_type = contract.AgentType,
            label,
            status = "spawned",
            message = $"Agent '{label}' has been spawned and is working on the task.",
        };

        return ToolResult.Success(JsonSerializer.Serialize(response));
    }
}
