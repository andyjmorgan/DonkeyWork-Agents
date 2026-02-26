using System.ComponentModel;
using System.Text.Json;
using DonkeyWork.Agents.Orleans.Contracts.Grains;
using DonkeyWork.Agents.Orleans.Contracts.Models;

namespace DonkeyWork.Agents.Orleans.Core.Tools.Swarm;

public sealed class SwarmAgentManagementTools
{
    [AgentTool("wait_for_any")]
    [Description("Wait for any spawned agent to complete and return its result. Returns the first agent that finishes. Use this when you have multiple agents running and want to process results as they arrive.")]
    public async Task<ToolResult> WaitForAny(
        [Description("Maximum seconds to wait before timing out. Defaults to 120.")]
        int timeout_seconds = 120,
        GrainContext? context = null,
        CancellationToken ct = default)
    {
        if (context is null)
        {
            return ToolResult.Error("GrainContext is required.");
        }

        var registry = GetRegistry(context);
        var timeout = TimeSpan.FromSeconds(timeout_seconds);

        var result = await registry.WaitForNextAsync(timeout);

        if (result is null)
        {
            return ToolResult.Success(JsonSerializer.Serialize(new
            {
                status = "timeout",
                message = $"No agent completed within {timeout_seconds} seconds.",
            }));
        }

        return FormatWaitResult(result);
    }

    [AgentTool("wait_for_agent")]
    [Description("Wait for a specific spawned agent to complete and return its result. Use this when you need the result of a particular agent before proceeding.")]
    public async Task<ToolResult> WaitForAgent(
        [Description("The agent key of the agent to wait for")]
        string agent_key,
        [Description("Maximum seconds to wait before timing out. Defaults to 120.")]
        int timeout_seconds = 120,
        GrainContext? context = null,
        CancellationToken ct = default)
    {
        if (context is null)
        {
            return ToolResult.Error("GrainContext is required.");
        }

        var registry = GetRegistry(context);
        var timeout = TimeSpan.FromSeconds(timeout_seconds);

        var result = await registry.WaitForSpecificAsync(agent_key, timeout);

        if (result is null)
        {
            return ToolResult.Success(JsonSerializer.Serialize(new
            {
                status = "timeout",
                agent_key,
                message = $"Agent did not complete within {timeout_seconds} seconds.",
            }));
        }

        return FormatWaitResult(result);
    }

    [AgentTool("cancel_agent")]
    [Description("Cancel a running agent. Use this to stop an agent that is no longer needed or is taking too long.")]
    public async Task<ToolResult> CancelAgent(
        [Description("The agent key of the agent to cancel")]
        string agent_key,
        GrainContext? context = null,
        CancellationToken ct = default)
    {
        if (context is null)
        {
            return ToolResult.Error("GrainContext is required.");
        }

        var grain = context.GrainFactory.GetGrain<IAgentGrain>(agent_key);
        await grain.CancelAsync();

        return ToolResult.Success(JsonSerializer.Serialize(new
        {
            agent_key,
            status = "cancel_requested",
            message = "Cancellation has been requested for the agent.",
        }));
    }

    [AgentTool("list_agents")]
    [Description("List all spawned agents in the current conversation with their current status. Use this to check on the progress of your spawned agents.")]
    public async Task<ToolResult> ListAgents(
        GrainContext? context = null,
        CancellationToken ct = default)
    {
        if (context is null)
        {
            return ToolResult.Error("GrainContext is required.");
        }

        var registry = GetRegistry(context);
        var agents = await registry.ListAsync();

        var agentList = agents.Select(a => new
        {
            agent_key = a.AgentKey,
            label = a.Label,
            status = a.Status.ToString().ToLowerInvariant(),
            spawned_at = a.SpawnedAt.ToString("O"),
            has_result = a.Result is not null,
        }).ToList();

        return ToolResult.Success(JsonSerializer.Serialize(new
        {
            count = agentList.Count,
            agents = agentList,
        }));
    }

    private static IAgentRegistryGrain GetRegistry(GrainContext context)
    {
        var conversationId = Guid.Parse(context.ConversationId);
        var registryKey = AgentKeys.Conversation(context.UserId, conversationId);
        return context.GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);
    }

    private static ToolResult FormatWaitResult(AgentWaitResult result)
    {
        var parts = result.Result.Parts.Select(p => p switch
        {
            AgentTextPart text => text.Text,
            AgentCitationPart citation => $"[{citation.Title}]({citation.Url}): {citation.CitedText}",
            _ => p.ToString() ?? string.Empty,
        });

        var content = string.Join("\n", parts);

        var response = new
        {
            agent_key = result.AgentKey,
            label = result.Label,
            status = result.Status.ToString().ToLowerInvariant(),
            content,
        };

        return ToolResult.Success(JsonSerializer.Serialize(response));
    }
}
