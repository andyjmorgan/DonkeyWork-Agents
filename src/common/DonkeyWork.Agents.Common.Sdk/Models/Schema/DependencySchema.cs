using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Common.Sdk.Models.Schema;

/// <summary>
/// Schema for field dependencies (legacy compatibility with DependsOn).
/// </summary>
public sealed class DependencySchema
{
    /// <summary>
    /// The field name this depends on (camelCase).
    /// </summary>
    [JsonPropertyName("field")]
    public required string Field { get; init; }

    /// <summary>
    /// The value required for this field to be visible.
    /// </summary>
    [JsonPropertyName("value")]
    public required object Value { get; init; }
}
