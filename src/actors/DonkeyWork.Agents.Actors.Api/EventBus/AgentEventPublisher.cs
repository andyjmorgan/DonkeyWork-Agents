using DonkeyWork.Agents.Actors.Contracts.Events;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.Common.MessageBus.Transport;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;
using NATS.Client.ObjectStore;

namespace DonkeyWork.Agents.Actors.Api.EventBus;

/// <summary>
/// Singleton that publishes stream events to JetStream using a per-type envelope.
/// Lazy-initializes the object store on first publish so startup ordering doesn't matter.
/// </summary>
public sealed class AgentEventPublisher : IAgentEventPublisher, IAsyncDisposable
{
    private readonly INatsJSContext _js;
    private readonly INatsObjContext _obj;
    private readonly IPayloadSerializer _serializer;
    private readonly PayloadTypeRegistry _registry;
    private readonly ILogger<AgentEventPublisher> _log;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private INatsObjStore? _store;
    private long _sequence;

    public AgentEventPublisher(
        INatsJSContext js,
        INatsObjContext obj,
        IPayloadSerializer serializer,
        PayloadTypeRegistry registry,
        ILogger<AgentEventPublisher> log)
    {
        _js = js;
        _obj = obj;
        _serializer = serializer;
        _registry = registry;
        _log = log;
    }

    public async Task PublishAsync(StreamEventBase evt, string conversationId, CancellationToken ct = default)
    {
        await EnsureStoreAsync(ct);

        var subject = AgentEventSubjects.ForEvent(conversationId, evt.TurnId);
        var seq = Interlocked.Increment(ref _sequence);
        var messageId = Guid.NewGuid().ToString("N");
        var payloadBytes = _serializer.Serialize(evt);

        Envelope envelope;
        if (payloadBytes.Length > 786432 && _store is not null)
        {
            var key = messageId;
            await using var ms = new MemoryStream(payloadBytes);
            await _store.PutAsync(key, ms, cancellationToken: ct);
            envelope = new Envelope
            {
                Sequence = seq,
                MessageId = messageId,
                TypeDiscriminator = evt.GetType().Name,
                Mode = PayloadMode.Stashed,
                ObjectRef = new ObjectRef { Bucket = AgentEventSubjects.BucketName, Key = key, Size = payloadBytes.Length }
            };
        }
        else
        {
            envelope = new Envelope
            {
                Sequence = seq,
                MessageId = messageId,
                TypeDiscriminator = evt.GetType().Name,
                Mode = PayloadMode.Inline,
                InlinePayload = payloadBytes
            };
        }

        var envelopeBytes = _serializer.Serialize(envelope);
        await _js.PublishAsync(subject, envelopeBytes, cancellationToken: ct);

        _log.LogDebug("Published {EventType} for conv={ConversationId} turn={TurnId} seq={Seq}",
            evt.EventType, conversationId, evt.TurnId, seq);
    }

    private async Task EnsureStoreAsync(CancellationToken ct)
    {
        if (_store is not null) return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_store is not null) return;
            _store = await _obj.GetObjectStoreAsync(AgentEventSubjects.BucketName, ct);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _initLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
