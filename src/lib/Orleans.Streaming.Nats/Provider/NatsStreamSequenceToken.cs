using Orleans.Streams;

namespace Orleans.Streaming.Nats.Provider;

[GenerateSerializer]
[Alias("Orleans.Streaming.Nats.Provider.NatsStreamSequenceToken")]
public sealed class NatsStreamSequenceToken : StreamSequenceToken
{
    [Id(0)]
    public override long SequenceNumber { get; protected set; }

    [Id(1)]
    public override int EventIndex { get; protected set; }

    public ulong StreamSequence => (ulong)SequenceNumber;

    public NatsStreamSequenceToken()
    {
    }

    public NatsStreamSequenceToken(ulong streamSequence, int eventIndex = 0)
    {
        SequenceNumber = (long)streamSequence;
        EventIndex = eventIndex;
    }

    public override bool Equals(StreamSequenceToken? other)
    {
        if (other is not NatsStreamSequenceToken natsToken)
            return false;

        return SequenceNumber == natsToken.SequenceNumber && EventIndex == natsToken.EventIndex;
    }

    public override int CompareTo(StreamSequenceToken? other)
    {
        if (other is not NatsStreamSequenceToken natsToken)
            throw new ArgumentException("Cannot compare to a non-NATS sequence token.", nameof(other));

        var result = SequenceNumber.CompareTo(natsToken.SequenceNumber);
        return result != 0 ? result : EventIndex.CompareTo(natsToken.EventIndex);
    }

    public override int GetHashCode() => HashCode.Combine(SequenceNumber, EventIndex);

    public override bool Equals(object? obj) => obj is NatsStreamSequenceToken token && Equals(token);

    public override string ToString() => $"NatsToken(Seq={StreamSequence}, EventIndex={EventIndex})";
}
