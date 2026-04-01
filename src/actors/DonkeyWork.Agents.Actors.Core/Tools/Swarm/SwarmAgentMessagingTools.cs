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


        ## Agent Communication

        You are part of a multi-agent swarm. Other agents may be running concurrently alongside you.

        **Receiving messages:** Messages from other agents arrive as `<agent-message from="name">` tags in the conversation. When you see one, read and act on it — it may contain instructions, findings, or coordination signals.

        **Sending messages:** Use `send_message` to contact another agent by name (e.g., `deep-researcher`, `delegate_2`). Use target `*` to broadcast to all active agents. Use this to share discoveries, request help, or coordinate work.

        **Shared context:** Use `write_shared_context` to publish key-value findings visible to all agents. Use `read_shared_context` to check what others have shared. This avoids duplicate work and enables collaboration.

        **Checking for messages:** Messages arrive automatically between tool calls. Use `check_messages` to explicitly poll if you want to check before continuing.
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

        await registry.SendMessageAsync(context.GrainKey, targetKey, agentMsg);
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
