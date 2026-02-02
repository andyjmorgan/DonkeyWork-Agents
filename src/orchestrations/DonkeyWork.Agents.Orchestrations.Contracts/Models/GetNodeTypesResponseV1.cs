using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Response containing available node types and their schemas.
/// </summary>
public sealed class GetNodeTypesResponseV1
{
    /// <summary>
    /// List of available node types.
    /// </summary>
    [JsonPropertyName("nodeTypes")]
    public required IReadOnlyList<NodeTypeInfo> NodeTypes { get; init; }
}
