using System.Text.Json;

namespace DonkeyWork.Agents.Common.MessageBus.Transport;

public sealed class JsonPayloadSerializer : IPayloadSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        IncludeFields = false,
        PropertyNameCaseInsensitive = true
    };

    public string Name => "Json";

    public byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);

    public T Deserialize<T>(byte[] bytes) => JsonSerializer.Deserialize<T>(bytes, Options)!;

    public object Deserialize(byte[] bytes, Type type) =>
        JsonSerializer.Deserialize(bytes, type, Options)!;
}
