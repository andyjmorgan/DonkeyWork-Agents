using MessagePack;

namespace DonkeyWork.Agents.Common.MessageBus.Transport;

public sealed class MessagePackPayloadSerializer : IPayloadSerializer
{
    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

    public string Name => "MessagePack";

    public byte[] Serialize<T>(T value) => MessagePackSerializer.Serialize(value, Options);

    public byte[] Serialize(object value, Type type) => MessagePackSerializer.Serialize(type, value, Options);

    public T Deserialize<T>(byte[] bytes) => MessagePackSerializer.Deserialize<T>(bytes, Options);

    public object Deserialize(byte[] bytes, Type type) =>
        MessagePackSerializer.Deserialize(type, bytes, Options)!;
}
