using Orleans.Streams;

namespace Orleans.Streaming.Nats.Provider;

[GenerateSerializer]
[Alias("Orleans.Streaming.Nats.Provider.NatsBatchContainer")]
public sealed class NatsBatchContainer : IBatchContainer
{
    [Id(0)]
    public StreamId StreamId { get; }

    [Id(1)]
    public NatsStreamSequenceToken NatsSequenceToken { get; }

    [Id(2)]
    public byte[] Payload { get; }

    [Id(3)]
    public Dictionary<string, object>? RequestContext { get; }

    public StreamSequenceToken SequenceToken => NatsSequenceToken;

    public NatsBatchContainer(
        StreamId streamId,
        byte[] payload,
        NatsStreamSequenceToken sequenceToken,
        Dictionary<string, object>? requestContext = null)
    {
        StreamId = streamId;
        Payload = payload;
        NatsSequenceToken = sequenceToken;
        RequestContext = requestContext;
    }

    public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
    {
        var item = System.Text.Json.JsonSerializer.Deserialize<T>(Payload)!;
        return [Tuple.Create(item, (StreamSequenceToken)NatsSequenceToken)];
    }

    public bool ImportRequestContext()
    {
        if (RequestContext is null)
            return false;

        foreach (var (key, value) in RequestContext)
        {
            Runtime.RequestContext.Set(key, value);
        }

        return true;
    }

    public override string ToString() =>
        $"NatsBatch(Stream={StreamId}, Token={NatsSequenceToken}, PayloadSize={Payload.Length})";
}
