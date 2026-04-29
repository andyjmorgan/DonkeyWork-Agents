using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Actors.Api.Observers;
using DonkeyWork.Agents.Actors.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StreamJsonRpc;

namespace DonkeyWork.Agents.Actors.Api.Endpoints;

public static class ConversationWebSocketEndpoints
{
    public static IEndpointRouteBuilder MapConversationWebSocket(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/conversations/{id:guid}/ws", HandleWebSocketAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task HandleWebSocketAsync(
        HttpContext context,
        Guid id,
        IGrainFactory grainFactory,
        IIdentityContext identityContext)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (!identityContext.IsAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var ws = await context.WebSockets.AcceptWebSocketAsync();

        var formatter = new SystemTextJsonFormatter
        {
            JsonSerializerOptions =
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() },
                AllowOutOfOrderMetadataProperties = true,
            },
        };

        var handler = new WebSocketMessageHandler(ws, formatter);
        using var rpc = new JsonRpc(handler);
        using var observer = new WebSocketObserver(rpc);

        var observerRef = grainFactory.CreateObjectReference<IAgentResponseObserver>(observer);
        var grainKey = $"{AgentKeys.ConversationPrefix}{identityContext.UserId}:{id}";

        RequestContext.Set(GrainCallContextKeys.UserId, identityContext.UserId.ToString());
        RequestContext.Set(GrainCallContextKeys.ConversationId, id.ToString());

        var grain = grainFactory.GetGrain<IConversationGrain>(grainKey);

        await grain.SubscribeAsync(observerRef);

        rpc.AddLocalRpcTarget(
            new ConversationRpcTarget(grain, observerRef, identityContext.UserId.ToString(), id.ToString(), grainKey),
            new JsonRpcTargetOptions { MethodNameTransform = CommonMethodNameTransforms.CamelCase });

        rpc.StartListening();
        await rpc.Completion;
    }
}

internal sealed class ConversationRpcTarget(IConversationGrain grain, IAgentResponseObserver observer, string userId, string conversationId, string grainKey)
{
    public async Task<object> Message(string text)
    {
        SetCallContext();
        var turnId = Guid.NewGuid();
        await grain.PostUserMessageAsync(text, turnId);
        return new { status = "queued", turnId };
    }

    /// <summary>
    /// Client notification: { jsonrpc: "2.0", method: "cancel", params: { key: "...", scope?: "active" } }
    /// The frontend may send a key like "swarm:{conversationId}" for self-cancel.
    /// Resolve any non-prefixed key to the actual grain key so CancelByKeyAsync can match.
    /// </summary>
    public Task Cancel(string key, string? scope = null)
    {
        SetCallContext();
        // If the key doesn't match a known agent prefix, treat it as a self-cancel
        var resolvedKey = key.StartsWith(AgentKeys.DelegatePrefix)
                          || key.StartsWith(AgentKeys.AgentPrefix)
                          || key.StartsWith(AgentKeys.ConversationPrefix)
                          || key.StartsWith(AgentKeys.TestPrefix)
            ? key
            : grainKey;
        return grain.CancelByKeyAsync(resolvedKey, scope);
    }

    /// <summary>
    /// Client request: { jsonrpc: "2.0", id: N, method: "listAgents" }
    /// </summary>
    public async Task<IReadOnlyList<TrackedAgent>> ListAgents()
    {
        SetCallContext();
        return await grain.ListAgentsAsync();
    }

    /// <summary>
    /// Client request: { jsonrpc: "2.0", id: N, method: "getState" }
    /// Returns the current conversation state including all messages.
    /// Pre-serializes messages to preserve $type polymorphic discriminators
    /// that would be lost through StreamJsonRpc's object serialization.
    /// </summary>
    public async Task<object> GetState()
    {
        SetCallContext();
        var messages = await grain.GetMessagesAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
            AllowOutOfOrderMetadataProperties = true,
        };
        var messagesJson = JsonSerializer.SerializeToElement(messages, options);
        return new { messages = messagesJson };
    }

    /// <summary>
    /// Client request: { jsonrpc: "2.0", id: N, method: "getAgentMessages", params: { agentKey: "..." } }
    /// Returns the full execution transcript for a specific sub-agent.
    /// </summary>
    public async Task<object> GetAgentMessages(string agentKey)
    {
        SetCallContext();
        var messages = await grain.GetAgentMessagesAsync(agentKey);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
            AllowOutOfOrderMetadataProperties = true,
        };
        var messagesJson = JsonSerializer.SerializeToElement(messages, options);
        return new { messages = messagesJson };
    }

    private void SetCallContext()
    {
        RequestContext.Set(GrainCallContextKeys.UserId, userId);
        RequestContext.Set(GrainCallContextKeys.ConversationId, conversationId);
    }
}
