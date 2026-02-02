using System.Text.Json.Serialization;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Schema;

namespace DonkeyWork.Agents.Agents.Contracts.Models;

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
