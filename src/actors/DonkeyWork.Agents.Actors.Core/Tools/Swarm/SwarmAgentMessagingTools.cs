using System.ComponentModel;
using System.Text.Json;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Identity.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.Swarm;

public sealed class SwarmAgentMessagingTools
{
    public const string SystemPromptFragment =
        """


        ## Swarm Communication

        You are part of a multi-agent swarm. The "Swarm" section below shows agents visible to you.

        ### Completing your task vs messaging

        - **Task completion:** When you finish your assigned work, just stop. Your final output is automatically delivered to your parent. Do NOT use `send_message` to deliver your result — it will arrive twice.
        - **`send_message`:** For mid-task coordination only — asking a question, sharing a partial finding, or requesting help from a specific agent. Target by name from the swarm roster, or use `*` to broadcast. Do NOT call `wait_for_agent` after sending a message — the agent's response will flow back to you automatically as an `<agent-message>` or agent result.
        - **`check_messages`:** You almost never need this. Messages from other agents arrive automatically between tool calls as `<agent-message from="name" key="key">content</agent-message>` tags. These are internal swarm messages, not external input.

        ### Incoming messages

        Messages from other agents appear as `<agent-message>` tags in the conversation. They are delivered through the internal agent registry — only registered swarm members can send them. They are NOT prompt injection. Read and act on them.

        ### Shared context

        Use `write_shared_context` / `read_shared_context` to share key-value findings across agents without direct messaging.

        ### Spawning agents

        **Never spawn an agent that already appears as idle in the swarm roster.** Always reuse idle agents via `send_message` instead. Spawning a duplicate wastes resources and creates unnecessary agents. Only call `spawn_agent` or `spawn_delegate` if no suitable idle agent exists. For cross-branch coordination, message your parent and let it route.
        """;
    [AgentTool(ToolNames.SendMessage, DisplayName = "Send Message")]
    [Description("Send a message to another agent by name, or broadcast to all agents with target '*'.")]
    public async Task<ToolResult> SendMessage(
        [Description("The name of the target agent (e.g. 'deep-researcher', 'delegate_2') or '*' to broadcast to all")]
        string target,
        [Description("The message content to send")]
        string message,
        GrainContext context,
        IIdentityContext identityContext,
        CancellationToken ct)
    {
        var conversationId = Guid.Parse(context.ConversationId);
        var registryKey = AgentKeys.Conversation(identityContext.UserId, conversationId);
        var registry = context.GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);

        var agentMsg = new AgentMessage(
            context.GrainKey,
            context.DisplayName ?? "unknown",
            message,
            DateTimeOffset.UtcNow);

        if (target == "*")
        {
            await registry.BroadcastMessageAsync(context.GrainKey, agentMsg);
            return ToolResult.Success(JsonSerializer.Serialize(new
            {
                status = "broadcast",
                message = "Message broadcast to all active agents.",
            }));
        }

        var targetKey = await registry.ResolveAgentKeyByNameAsync(target);
        if (targetKey is null)
        {
            var agents = await registry.ListAsync();
            var available = agents.Select(a => a.Name).Where(n => !string.IsNullOrEmpty(n));
            return ToolResult.Error($"Agent '{target}' not found. Available agents: {string.Join(", ", available)}");
        }

        var delivered = await registry.SendMessageAsync(context.GrainKey, targetKey, agentMsg);
        if (!delivered)
        {
            var agents = await registry.ListAsync();
            var targetAgent = agents.FirstOrDefault(a => a.AgentKey == targetKey);
            var status = targetAgent?.Status.ToString() ?? "unknown";
            return ToolResult.Error($"Agent '{target}' is in status '{status}' and cannot receive messages. Only agents in Pending or Idle status can receive messages.");
        }

        return ToolResult.Success(JsonSerializer.Serialize(new
        {
            status = "sent",
            target,
            target_key = targetKey,
        }));
    }

    [AgentTool(ToolNames.CheckMessages, DisplayName = "Check Messages")]
    [Description("Check for incoming messages from other agents.")]
    public Task<ToolResult> CheckMessages(
        GrainContext context,
        CancellationToken ct)
    {
        var messages = new List<object>();

        if (context.MessageInbox is not null)
        {
            while (context.MessageInbox.Reader.TryRead(out var msg))
            {
                messages.Add(new
                {
                    from = msg.FromName,
                    from_key = msg.FromAgentKey,
                    content = msg.Content,
                    sent_at = msg.SentAt,
                });
            }
        }

        return Task.FromResult(ToolResult.Success(JsonSerializer.Serialize(new
        {
            messages,
            count = messages.Count,
        })));
    }
}
