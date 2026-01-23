using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Models;

public sealed class ModelCatalog
{
    [JsonPropertyName("models")]
    public required IReadOnlyList<ModelDefinition> Models { get; init; }
}
