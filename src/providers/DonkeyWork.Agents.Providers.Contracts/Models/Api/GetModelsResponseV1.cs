using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Models.Api;

/// <summary>
/// Response containing a list of available models.
/// </summary>
public sealed class GetModelsResponseV1
{
    [JsonPropertyName("models")]
    public required IReadOnlyList<ModelDefinition> Models { get; init; }
}
