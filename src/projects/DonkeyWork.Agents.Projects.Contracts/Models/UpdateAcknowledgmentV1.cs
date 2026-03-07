using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Simple acknowledgment response for update operations, avoiding the overhead of returning the full entity.
/// </summary>
public sealed class UpdateAcknowledgmentV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }
}
