using System.Text.Json;

namespace DonkeyWork.Agents.Actors.Contracts.Messages;

[GenerateSerializer]
public struct JsonElementSurrogate
{
    [Id(0)] public string Json { get; set; }
}

[RegisterConverter]
public sealed class JsonElementSurrogateConverter :
    IConverter<JsonElement, JsonElementSurrogate>
{
    public JsonElement ConvertFromSurrogate(in JsonElementSurrogate surrogate)
    {
        if (string.IsNullOrEmpty(surrogate.Json))
            return default;

        using var doc = JsonDocument.Parse(surrogate.Json);
        return doc.RootElement.Clone();
    }

    public JsonElementSurrogate ConvertToSurrogate(in JsonElement value)
    {
        return new JsonElementSurrogate { Json = value.GetRawText() };
    }
}
