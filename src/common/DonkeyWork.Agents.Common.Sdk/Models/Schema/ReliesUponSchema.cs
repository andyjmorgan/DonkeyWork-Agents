using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Common.Sdk.Models.Schema;

/// <summary>
/// Schema for ReliesUpon conditional visibility.
/// </summary>
public sealed class ReliesUponSchema
{
    /// <summary>
    /// The field name this depends on (camelCase).
    /// </summary>
    [JsonPropertyName("fieldName")]
    public required string FieldName { get; init; }

    /// <summary>
    /// The value the parent field must have for this field to be visible.
    /// </summary>
    [JsonPropertyName("value")]
    public required object Value { get; init; }

    /// <summary>
    /// When enabled, is this field required?
    /// </summary>
    [JsonPropertyName("requiredWhenEnabled")]
    public bool RequiredWhenEnabled { get; init; }
}
