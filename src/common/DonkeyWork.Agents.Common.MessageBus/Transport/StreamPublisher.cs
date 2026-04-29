using System.Diagnostics;
using DonkeyWork.Agents.Common.MessageBus.Payloads;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;
using NATS.Client.ObjectStore;

namespace DonkeyWork.Agents.Common.MessageBus.Transport;

public sealed class StreamPublisher
{
    private readonly INatsJSContext _js;
    private readonly INatsObjContext _obj;
    private readonly IPayloadSerializer _serializer;
    private readonly TransportOptions _options;
    private readonly ILogger<StreamPublisher> _log;
    private INatsObjStore? _store;
    private long _sequence;

    public StreamPublisher(
        INatsJSContext js,
        INatsObjContext obj,
        IPayloadSerializer serializer,
        TransportOptions options,
        ILogger<StreamPublisher> log)
    {
        _js = js;
        _obj = obj;
        _serializer = serializer;
        _options = options;
        _log = log;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        _store = await _obj.GetObjectStoreAsync(_options.Bucket, ct);
    }

    public async Task PublishAsync<T>(T payload, string subject, CancellationToken ct) where T : IPayload
    {
        if (_store is null) throw new InvalidOperationException("Call InitializeAsync before publishing.");

        var seq = _sequence + 1;
        var messageId = Guid.NewGuid().ToString("N");
        var payloadBytes = _serializer.Serialize(payload);

        var shortId = HumanFormat.ShortId(messageId);
        var size = HumanFormat.Bytes(payloadBytes.Length);
        var totalSw = Stopwatch.GetTimestamp();

        Envelope envelope;
        string icon;
        if (payloadBytes.Length > _options.StashThresholdBytes)
        {
            var key = messageId;
            await using (var ms = new MemoryStream(payloadBytes))
            {
                await _store.PutAsync(key, ms, cancellationToken: ct);
            }
            envelope = new Envelope
            {
                Sequence = seq,
                MessageId = messageId,
                TypeDiscriminator = payload.Discriminator,
                Mode = PayloadMode.Stashed,
                ObjectRef = new ObjectRef { Bucket = _options.Bucket, Key = key, Size = payloadBytes.Length }
            };
            icon = "stashed";
        }
        else
        {
            envelope = new Envelope
            {
                Sequence = seq,
                MessageId = messageId,
                TypeDiscriminator = payload.Discriminator,
                Mode = PayloadMode.Inline,
                InlinePayload = payloadBytes
            };
            icon = "inline";
        }

        var envelopeBytes = _serializer.Serialize(envelope);
        await _js.PublishAsync(subject, envelopeBytes, cancellationToken: ct);
        _sequence++;

        var totalMs = Stopwatch.GetElapsedTime(totalSw).TotalMilliseconds;

        _log.LogDebug("PUB ({Mode}) #{Seq} {Type} {Size} ({Elapsed}ms) [{Id}]",
            icon, seq, payload.Discriminator, size, HumanFormat.Ms(totalMs), shortId);
    }
}
