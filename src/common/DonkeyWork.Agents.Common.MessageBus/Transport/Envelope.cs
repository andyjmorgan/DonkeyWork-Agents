using MessagePack;

namespace DonkeyWork.Agents.Common.MessageBus.Transport;

public enum PayloadMode { Inline = 0, Stashed = 1 }

[MessagePackObject(true)]
public sealed class Envelope
{
    public long Sequence { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string TypeDiscriminator { get; set; } = string.Empty;
    public PayloadMode Mode { get; set; }
    public byte[]? InlinePayload { get; set; }
    public ObjectRef? ObjectRef { get; set; }
}

[MessagePackObject(true)]
public sealed class ObjectRef
{
    public string Bucket { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public long Size { get; set; }
}
