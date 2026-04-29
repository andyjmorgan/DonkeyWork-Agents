using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Actors.Api.EventBus;
using DonkeyWork.Agents.Actors.Api.Observers;
using DonkeyWork.Agents.Actors.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Events;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Common.MessageBus.Transport;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.JetStream.Models;
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
        IIdentityContext identityContext,
        AgentEventConsumerFactory consumerFactory)
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
        using var cts = new CancellationTokenSource();

        var grainKey = $"{AgentKeys.ConversationPrefix}{identityContext.UserId}:{id}";
        var conversationId = id.ToString();

        RequestContext.Set(GrainCallContextKeys.UserId, identityContext.UserId.ToString());
        RequestContext.Set(GrainCallContextKeys.ConversationId, conversationId);

        var grain = grainFactory.GetGrain<IConversationGrain>(grainKey);

        rpc.AddLocalRpcTarget(
            new ConversationRpcTarget(grain, identityContext.UserId.ToString(), conversationId, grainKey, consumerFactory),
            new JsonRpcTargetOptions { MethodNameTransform = CommonMethodNameTransforms.CamelCase });

        rpc.StartListening();

        var consumer = consumerFactory.CreateConsumer();
        var liveOpts = AgentEventConsumerFactory.LiveTailOptions(conversationId);

        var forwardTask = ForwardEventsAsync(consumer, liveOpts, rpc, cts.Token);

        await rpc.Completion;

        cts.Cancel();
        try { await forwardTask; } catch (OperationCanceledException) { }
    }

    private static async Task ForwardEventsAsync(
        StreamConsumer consumer,
        ConsumerOptions opts,
        JsonRpc rpc,
        CancellationToken ct)
    {
        await foreach (var delivered in consumer.GetMessagesAsync<StreamEventBase>(opts, ct))
        {
            if (rpc.IsDisposed) break;
            try
            {
                var payload = ToJsonElement(delivered.Payload);
                await rpc.NotifyAsync("event", payload, delivered.Sequence);
            }
            catch
            {
                break;
            }
        }
    }

    internal static JsonElement ToJsonElement(StreamEventBase evt)
    {
        var bytes = evt switch
        {
            StreamMessageEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamMessageEvent),
            StreamThinkingEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamThinkingEvent),
            StreamToolUseEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamToolUseEvent),
            StreamToolResultEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamToolResultEvent),
            StreamToolCompleteEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamToolCompleteEvent),
            StreamWebSearchEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamWebSearchEvent),
            StreamWebSearchCompleteEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamWebSearchCompleteEvent),
            StreamCitationEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamCitationEvent),
            StreamUsageEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamUsageEvent),
            StreamProgressEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamProgressEvent),
            StreamAgentSpawnEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamAgentSpawnEvent),
            StreamAgentCompleteEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamAgentCompleteEvent),
            StreamAgentResultDataEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamAgentResultDataEvent),
            StreamCompleteEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamCompleteEvent),
            StreamErrorEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamErrorEvent),
            StreamRetryEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamRetryEvent),
            StreamTurnStartEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamTurnStartEvent),
            StreamTurnEndEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamTurnEndEvent),
            StreamQueueStatusEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamQueueStatusEvent),
            StreamCancelledEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamCancelledEvent),
            StreamMcpServerStatusEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamMcpServerStatusEvent),
            StreamSandboxStatusEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamSandboxStatusEvent),
            _ => JsonSerializer.SerializeToUtf8Bytes(new { agentKey = evt.AgentKey, eventType = evt.EventType }),
        };

        using var doc = JsonDocument.Parse(bytes);
        return doc.RootElement.Clone();
    }
}

internal sealed class ConversationRpcTarget(
    IConversationGrain grain,
    string userId,
    string conversationId,
    string grainKey,
    AgentEventConsumerFactory consumerFactory)
{
    public async Task<object> Message(string text)
    {
        SetCallContext();
        var turnId = Guid.NewGuid();
        await grain.PostUserMessageAsync(text, turnId);
        return new { status = "queued", turnId };
    }

    /// <summary>
    /// Cancels a specific pending or active turn by its id.
    /// </summary>
    public async Task<object> CancelTurn(string turnId)
    {
        SetCallContext();
        if (!Guid.TryParse(turnId, out var id))
            return new { result = "notFound" };
        var outcome = await grain.CancelTurnAsync(id);
        return new { result = outcome.ToString().ToLowerInvariant() };
    }

    /// <summary>
    /// Cancels all active or pending turns for an agent key.
    /// </summary>
    public Task Cancel(string key, string? scope = null)
    {
        SetCallContext();
        var resolvedKey = key.StartsWith(AgentKeys.DelegatePrefix)
                          || key.StartsWith(AgentKeys.AgentPrefix)
                          || key.StartsWith(AgentKeys.ConversationPrefix)
                          || key.StartsWith(AgentKeys.TestPrefix)
            ? key
            : grainKey;
        return grain.CancelByKeyAsync(resolvedKey, scope);
    }

    public async Task<IReadOnlyList<TrackedAgent>> ListAgents()
    {
        SetCallContext();
        return await grain.ListAgentsAsync();
    }

    /// <summary>
    /// Returns the current conversation state including all persisted messages.
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

    /// <summary>
    /// Replays events for a turn from a given JetStream sequence cursor.
    /// Returns collected events, the last sequence seen, and whether the turn is complete.
    /// Used by clients after reconnect to catch up on missed events.
    /// </summary>
    public async Task<object> EventsSince(string turnId, ulong afterSequence)
    {
        SetCallContext();
        if (!Guid.TryParse(turnId, out var turnGuid))
            return new { events = Array.Empty<object>(), lastSequence = afterSequence, complete = false };

        var consumer = consumerFactory.CreateConsumer();
        var opts = AgentEventConsumerFactory.ReplayFromOptions(conversationId, turnGuid, afterSequence);

        var events = new List<ReplayedEvent>();
        ulong lastSeq = afterSequence;
        bool complete = false;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(2000));

        try
        {
            await foreach (var delivered in consumer.GetMessagesAsync<StreamEventBase>(opts, cts.Token))
            {
                var element = ConversationWebSocketEndpoints.ToJsonElement(delivered.Payload);
                events.Add(new ReplayedEvent(element, delivered.Sequence));
                lastSeq = delivered.Sequence;

                if (delivered.Payload is StreamTurnEndEvent or StreamCancelledEvent)
                {
                    complete = true;
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
        };
        return new
        {
            events = JsonSerializer.SerializeToElement(events, jsonOptions),
            lastSequence = lastSeq,
            complete,
        };
    }

    private sealed record ReplayedEvent(JsonElement Payload, ulong Sequence);

    private void SetCallContext()
    {
        RequestContext.Set(GrainCallContextKeys.UserId, userId);
        RequestContext.Set(GrainCallContextKeys.ConversationId, conversationId);
    }
}
