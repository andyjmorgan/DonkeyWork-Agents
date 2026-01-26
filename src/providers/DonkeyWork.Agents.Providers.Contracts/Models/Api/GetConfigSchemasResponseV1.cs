using System.Text.Json.Serialization;
using DonkeyWork.Agents.Providers.Contracts.Models.Schema;

namespace DonkeyWork.Agents.Providers.Contracts.Models.Api;

/// <summary>
/// Response containing configuration schemas for all models.
/// </summary>
public sealed class GetConfigSchemasResponseV1
{
    [JsonPropertyName("schemas")]
    public required IReadOnlyDictionary<string, ModelConfigSchema> Schemas { get; init; }
}
