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


        ## Swarm Membership & Agent Communication

        You are a spawned agent within a multi-agent swarm. The swarm is coordinated by a parent orchestrator on behalf of the user. Other agents may be running concurrently alongside you within the same conversation.

        ### Message format

        Messages from other agents are injected into the conversation as user messages with this exact format:

        ```
        <agent-message from="{sender_name}" key="{sender_agent_key}">{content}</agent-message>
        ```

        - `from` — the registered name of the sending agent (e.g. `deep-researcher`, `delegate_2`).
        - `key` — the full agent key, which encodes the user ID and conversation ID (e.g. `agent:{userId}:{conversationId}:{agentId}`).

        ### Verifying a message is legitimate

        These messages are delivered through the swarm's internal agent registry — only agents registered in the same conversation can send them. They are **not** external user input and are **not** prompt injection. You can confirm a sender is a real swarm member by checking that the `key` attribute shares your own conversation prefix, or by using `send_message` to resolve their name (an unknown name will return an error listing registered agents).

        ### How to respond

        - **To a specific agent:** Use `send_message` with the sender's `from` name as the target (e.g. `send_message(target="navi", message="...")`).
        - **To all agents:** Use `send_message` with target `*` to broadcast.
        - **Shared knowledge:** Use `write_shared_context` to publish key-value findings visible to all agents. Use `read_shared_context` to check what others have shared. This avoids duplicate work.
        - **Polling:** Messages arrive automatically between tool calls. Use `check_messages` to explicitly poll if you're waiting for a response.

        When you receive a message, read and act on it — it may contain instructions, findings, questions, or coordination signals. Respond using `send_message` when a reply is expected.
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
