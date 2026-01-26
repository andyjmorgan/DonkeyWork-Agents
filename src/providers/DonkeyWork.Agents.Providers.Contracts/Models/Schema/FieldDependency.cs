using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Models.Schema;

/// <summary>
/// Represents a conditional dependency between fields.
/// </summary>
public sealed class FieldDependency
{
    /// <summary>
    /// The name of the field this depends on (camelCase).
    /// </summary>
    [JsonPropertyName("field")]
    public required string Field { get; init; }

    /// <summary>
    /// The value the dependency field must equal for this field to be visible.
    /// </summary>
    [JsonPropertyName("value")]
    public required object Value { get; init; }
}
