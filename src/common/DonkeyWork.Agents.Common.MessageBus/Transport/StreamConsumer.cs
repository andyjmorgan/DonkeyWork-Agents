using System.Diagnostics;
using System.Runtime.CompilerServices;
using DonkeyWork.Agents.Common.MessageBus.Payloads;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.ObjectStore;

namespace DonkeyWork.Agents.Common.MessageBus.Transport;

public sealed class StreamConsumer
{
    private readonly INatsJSContext _js;
    private readonly INatsObjContext _obj;
    private readonly IPayloadSerializer _serializer;
    private readonly PayloadTypeRegistry _registry;
    private readonly TransportOptions _options;
    private readonly ILogger<StreamConsumer> _log;

    public StreamConsumer(
        INatsJSContext js,
        INatsObjContext obj,
        IPayloadSerializer serializer,
        PayloadTypeRegistry registry,
        TransportOptions options,
        ILogger<StreamConsumer> log)
    {
        _js = js;
        _obj = obj;
        _serializer = serializer;
        _registry = registry;
        _options = options;
        _log = log;
    }

    private async Task<byte[]> FetchWithRetryAsync(INatsObjStore store, string key, string messageId, CancellationToken ct)
    {
        const int maxAttempts = 4;
        var delayMs = 100;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var ms = new MemoryStream();
                await store.GetAsync(key, ms, cancellationToken: ct);
                if (attempt > 1)
                    _log.LogInformation("fetch recovered after {Attempt} attempts [{Id}]",
                        attempt, HumanFormat.ShortId(messageId));
                return ms.ToArray();
            }
            catch (Exception ex) when (attempt < maxAttempts && ex.Message.Contains("SHA-256", StringComparison.OrdinalIgnoreCase))
            {
                _log.LogWarning("SHA mismatch on attempt {Attempt}/{Max}, retrying in {Delay}ms [{Id}]",
                    attempt, maxAttempts, delayMs, HumanFormat.ShortId(messageId));
                await Task.Delay(delayMs, ct);
                delayMs *= 2;
            }
        }
    }

    /// <summary>
    /// Streams messages from the subject. When <paramref name="startSequence"/> is provided,
    /// delivery begins from that JetStream sequence number; otherwise only new messages are delivered.
    /// Each yielded <see cref="DeliveredMessage{T}"/> carries the JetStream sequence number so callers
    /// can persist a cursor for reconnect.
    /// </summary>
    public async IAsyncEnumerable<DeliveredMessage<T>> GetMessagesAsync<T>(
        ConsumerOptions consumerOpts,
        [EnumeratorCancellation] CancellationToken ct)
        where T : IPayload
    {
        var store = await _obj.GetObjectStoreAsync(_options.Bucket, ct);

        var consumerName = $"{_options.Consumer}-{Guid.NewGuid():N}";
        var filterSubject = consumerOpts.FilterSubject ?? _options.SubjectFilter;

        var config = new ConsumerConfig(consumerName)
        {
            AckPolicy = ConsumerConfigAckPolicy.Explicit,
            DeliverPolicy = consumerOpts.DeliverPolicy,
            MaxDeliver = _options.MaxDeliver,
            AckWait = TimeSpan.FromSeconds(_options.AckWaitSeconds),
            FilterSubject = filterSubject,
        };

        if (consumerOpts.DeliverPolicy == ConsumerConfigDeliverPolicy.ByStartSequence && consumerOpts.OptStartSeq.HasValue)
            config.OptStartSeq = consumerOpts.OptStartSeq.Value;

        var consumer = await _js.CreateOrUpdateConsumerAsync(_options.Stream, config, ct);

        var consumeOpts = new NatsJSConsumeOpts
        {
            MaxMsgs = 20,
            Expires = TimeSpan.FromSeconds(30),
        };

        await foreach (var msg in consumer.ConsumeAsync<byte[]>(opts: consumeOpts, cancellationToken: ct))
        {
            if (msg.Data is null || msg.Data.Length == 0)
            {
                await msg.AckAsync(cancellationToken: ct);
                continue;
            }

            var totalSw = Stopwatch.GetTimestamp();
            IPayload? payload = null;
            Envelope? envelope = null;
            byte[] payloadBytes = Array.Empty<byte>();
            bool poison = false;
            string? failure = null;

            try
            {
                envelope = _serializer.Deserialize<Envelope>(msg.Data);

                if (!_registry.TryResolve(envelope.TypeDiscriminator, out _))
                {
                    poison = true;
                    failure = $"unknown discriminator '{envelope.TypeDiscriminator}'";
                }
                else if (envelope.Mode == PayloadMode.Stashed)
                {
                    if (envelope.ObjectRef is null)
                    {
                        poison = true;
                        failure = "stashed envelope missing objectRef";
                    }
                    else
                    {
                        payloadBytes = await FetchWithRetryAsync(store, envelope.ObjectRef.Key, envelope.MessageId, ct);
                    }
                }
                else
                {
                    payloadBytes = envelope.InlinePayload ?? Array.Empty<byte>();
                }

                if (!poison)
                {
                    payload = (IPayload)_serializer.Deserialize(payloadBytes, _registry.TryResolve(envelope.TypeDiscriminator, out var resolvedType) ? resolvedType : typeof(object));
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var nakSeq = envelope?.Sequence ?? 0;
                _log.LogWarning("process failed ({Reason}) — nak for redelivery (pub #{Seq})", ex.Message, nakSeq);
                try { await msg.NakAsync(cancellationToken: ct); } catch { }
                continue;
            }

            if (poison)
            {
                _log.LogError("poison message: {Reason} messageId={Id} — terminating",
                    failure, envelope?.MessageId);
                try { await msg.AckTerminateAsync(cancellationToken: ct); } catch { }
                continue;
            }

            await msg.AckAsync(cancellationToken: ct);

            var totalMs = Stopwatch.GetElapsedTime(totalSw).TotalMilliseconds;
            var jsSeq = msg.Metadata?.Sequence.Stream ?? 0;

            _log.LogDebug("RCV seq={Seq} type={Type} size={Size} elapsed={Elapsed}ms",
                jsSeq, envelope!.TypeDiscriminator,
                HumanFormat.Bytes(payloadBytes.Length), HumanFormat.Ms(totalMs));

            if (payload is T typed)
                yield return new DeliveredMessage<T> { Payload = typed, Sequence = jsSeq };
        }
    }
}
