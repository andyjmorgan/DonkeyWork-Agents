namespace DonkeyWork.Agents.Common.MessageBus.Transport;

public interface IPayloadSerializer
{
    string Name { get; }
    byte[] Serialize<T>(T value);
    byte[] Serialize(object value, Type type);
    T Deserialize<T>(byte[] bytes);
    object Deserialize(byte[] bytes, Type type);
}
