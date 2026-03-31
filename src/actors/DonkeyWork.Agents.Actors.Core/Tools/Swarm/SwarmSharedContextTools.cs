using System.ComponentModel;
using System.Text.Json;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Identity.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.Swarm;

public sealed class SwarmSharedContextTools
{
    [AgentTool(ToolNames.WriteSharedContext, DisplayName = "Write Shared Context")]
    [Description("Write a key-value pair to the shared context, accessible by all agents in this conversation.")]
    public async Task<ToolResult> WriteSharedContext(
        [Description("The key to write")]
        string key,
        [Description("The value to store")]
        string value,
        GrainContext context,
        IIdentityContext identityContext,
        CancellationToken ct)
    {
        var conversationId = Guid.Parse(context.ConversationId);
        var registryKey = AgentKeys.Conversation(identityContext.UserId, conversationId);
        var registry = context.GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);

        await registry.WriteSharedContextAsync(key, value);

        return ToolResult.Success(JsonSerializer.Serialize(new
        {
            status = "written",
            key,
        }));
    }

    [AgentTool(ToolNames.ReadSharedContext, DisplayName = "Read Shared Context")]
    [Description("Read from the shared context. Provide a key to read a specific value, or omit it to read all entries.")]
    public async Task<ToolResult> ReadSharedContext(
        [Description("The key to read. Omit to read all entries.")]
        string? key = null,
        GrainContext? context = null,
        IIdentityContext? identityContext = null,
        CancellationToken ct = default)
    {
        var conversationId = Guid.Parse(context!.ConversationId);
        var registryKey = AgentKeys.Conversation(identityContext!.UserId, conversationId);
        var registry = context.GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);

        if (key is not null)
        {
            var value = await registry.ReadSharedContextAsync(key);
            return ToolResult.Success(JsonSerializer.Serialize(new
            {
                key,
                value,
                found = value is not null,
            }));
        }

        var all = await registry.ReadAllSharedContextAsync();
        return ToolResult.Success(JsonSerializer.Serialize(new
        {
            entries = all,
            count = all.Count,
        }));
    }
}
