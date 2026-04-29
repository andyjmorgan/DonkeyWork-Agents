namespace DonkeyWork.Agents.Common.MessageBus.Transport;

/// <summary>
/// Wraps a deserialized payload together with its JetStream sequence number so consumers
/// can track a cursor for reconnect / EventsSince replay.
/// </summary>
public sealed class DeliveredMessage<T>
{
    public T Payload { get; init; } = default!;
    public ulong Sequence { get; init; }
}
