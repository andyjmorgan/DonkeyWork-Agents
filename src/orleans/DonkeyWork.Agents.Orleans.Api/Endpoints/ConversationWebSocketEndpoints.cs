using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orleans.Api.Observers;
using DonkeyWork.Agents.Orleans.Contracts.Grains;
using DonkeyWork.Agents.Orleans.Contracts.Models;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StreamJsonRpc;

namespace DonkeyWork.Agents.Orleans.Api.Endpoints;

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
        var grain = grainFactory.GetGrain<IConversationGrain>(grainKey);

        await grain.SubscribeAsync(observerRef);

        rpc.AddLocalRpcTarget(
            new ConversationRpcTarget(grain, observerRef),
            new JsonRpcTargetOptions { MethodNameTransform = CommonMethodNameTransforms.CamelCase });

        rpc.StartListening();
        await rpc.Completion;
    }
}

internal sealed class ConversationRpcTarget(IConversationGrain grain, IAgentResponseObserver observer)
{
    /// <summary>
    /// Client request: { jsonrpc: "2.0", id: N, method: "message", params: { text: "..." } }
    /// Re-subscribes the observer on every call so that if the grain deactivated during
    /// idle and lost its in-memory observer reference, events will flow again.
    /// </summary>
    public async Task<object> Message(string text)
    {
        await grain.SubscribeAsync(observer);
        await grain.PostUserMessageAsync(text);
        return new { status = "queued" };
    }

    /// <summary>
    /// Client notification: { jsonrpc: "2.0", method: "cancel", params: { key: "...", scope?: "active" } }
    /// </summary>
    public Task Cancel(string key, string? scope = null)
        => grain.CancelByKeyAsync(key, scope);

    /// <summary>
    /// Client request: { jsonrpc: "2.0", id: N, method: "listAgents" }
    /// </summary>
    public async Task<IReadOnlyList<TrackedAgent>> ListAgents()
    {
        return await grain.ListAgentsAsync();
    }
}
