using NATS.Client.JetStream.Models;

namespace DonkeyWork.Agents.Common.MessageBus.Transport;

public sealed class ConsumerOptions
{
    public ConsumerConfigDeliverPolicy DeliverPolicy { get; set; } = ConsumerConfigDeliverPolicy.New;

    /// <summary>
    /// When <see cref="DeliverPolicy"/> is <see cref="ConsumerConfigDeliverPolicy.ByStartSequence"/>,
    /// delivery starts from this JetStream sequence number.
    /// </summary>
    public ulong? OptStartSeq { get; set; }

    public string? FilterSubject { get; set; }
}
