using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Actors.Contracts.Models;

public sealed class NaviSettingsV1
{
    [JsonPropertyName("modelId")]
    public required string ModelId { get; init; }
}
