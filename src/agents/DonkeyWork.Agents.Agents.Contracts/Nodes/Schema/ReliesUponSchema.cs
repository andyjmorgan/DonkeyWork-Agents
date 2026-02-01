using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Schema;

/// <summary>
/// Schema definition for field conditional visibility.
/// </summary>
public sealed class ReliesUponSchema
{
    /// <summary>
    /// The name of the field this depends on (camelCase).
    /// </summary>
    [JsonPropertyName("fieldName")]
    public required string FieldName { get; init; }

    /// <summary>
    /// The value the dependent field must have for this field to be visible.
    /// </summary>
    [JsonPropertyName("value")]
    public required object Value { get; init; }

    /// <summary>
    /// Whether this field becomes required when visible.
    /// </summary>
    [JsonPropertyName("requiredWhenEnabled")]
    public bool RequiredWhenEnabled { get; init; }
}
