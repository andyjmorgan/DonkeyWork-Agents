using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Schema;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Response containing the multimodal chat model configuration schema.
/// </summary>
public sealed class GetMultimodalChatSchemaResponseV1
{
    /// <summary>
    /// The schema for the multimodal chat model configuration.
    /// </summary>
    [JsonPropertyName("schema")]
    public required MultimodalChatSchema Schema { get; init; }
}
