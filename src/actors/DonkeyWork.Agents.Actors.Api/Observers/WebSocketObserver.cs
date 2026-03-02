using System.Text.Json;
using System.Threading.Channels;
using DonkeyWork.Agents.Actors.Contracts.Events;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using StreamJsonRpc;

namespace DonkeyWork.Agents.Actors.Api.Observers;

/// <summary>
/// Bridges <see cref="IAgentResponseObserver"/> events to JSON-RPC 2.0 notifications over
/// a StreamJsonRpc connection. Events are queued in a <see cref="Channel{T}"/> and sent by a
/// single consumer loop, guaranteeing delivery order.
/// </summary>
public sealed class WebSocketObserver : IAgentResponseObserver, IDisposable
{
    private readonly JsonRpc _rpc;
    private readonly Channel<StreamEventBase> _queue = Channel.CreateUnbounded<StreamEventBase>(
        new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
    private readonly Task _sendLoop;

    public WebSocketObserver(JsonRpc rpc)
    {
        _rpc = rpc;
        _sendLoop = Task.Run(ProcessSendQueueAsync);
    }

    public void OnEvent(StreamEventBase streamEvent)
    {
        _queue.Writer.TryWrite(streamEvent);
    }

    private async Task ProcessSendQueueAsync()
    {
        await foreach (var evt in _queue.Reader.ReadAllAsync())
        {
            if (_rpc.IsDisposed) break;
            try
            {
                var payload = ToJsonElement(evt);
                await _rpc.NotifyAsync("event", payload);
            }
            catch
            {
                break;
            }
        }
    }

    private static JsonElement ToJsonElement(StreamEventBase evt)
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
            StreamTurnStartEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamTurnStartEvent),
            StreamTurnEndEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamTurnEndEvent),
            StreamQueueStatusEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamQueueStatusEvent),
            StreamCancelledEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamCancelledEvent),
            StreamMcpServerStatusEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamMcpServerStatusEvent),
            _ => JsonSerializer.SerializeToUtf8Bytes(new { agentKey = evt.AgentKey, eventType = evt.EventType }),
        };

        using var doc = JsonDocument.Parse(bytes);
        return doc.RootElement.Clone();
    }

    public void Dispose()
    {
        _queue.Writer.TryComplete();
    }
}
