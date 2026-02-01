using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Nodes.Enums;

namespace DonkeyWork.Agents.Common.Nodes.Schema;

/// <summary>
/// Schema definition for a configurable field.
/// </summary>
public sealed class FieldSchema
{
    /// <summary>
    /// The property name (camelCase).
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Display label for the field.
    /// </summary>
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    /// <summary>
    /// Optional description explaining the field's purpose.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    /// <summary>
    /// The underlying data type (string, number, boolean, array, object).
    /// </summary>
    [JsonPropertyName("propertyType")]
    public required string PropertyType { get; init; }

    /// <summary>
    /// The UI control type.
    /// </summary>
    [JsonPropertyName("controlType")]
    public required ControlType ControlType { get; init; }

    /// <summary>
    /// Display order within the form (lower values appear first).
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; init; }

    /// <summary>
    /// The tab this field belongs to (null for default tab).
    /// </summary>
    [JsonPropertyName("tab")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tab { get; init; }

    /// <summary>
    /// Whether this field is required.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; init; }

    /// <summary>
    /// Whether this field supports {{variable}} syntax.
    /// </summary>
    [JsonPropertyName("supportsVariables")]
    public bool SupportsVariables { get; init; }

    /// <summary>
    /// Placeholder text for the field input.
    /// </summary>
    [JsonPropertyName("placeholder")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Placeholder { get; init; }

    /// <summary>
    /// The default value for the field.
    /// </summary>
    [JsonPropertyName("default")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Minimum value (for slider/number fields).
    /// </summary>
    [JsonPropertyName("min")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Min { get; init; }

    /// <summary>
    /// Maximum value (for slider/number fields).
    /// </summary>
    [JsonPropertyName("max")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Max { get; init; }

    /// <summary>
    /// Step value (for slider fields).
    /// </summary>
    [JsonPropertyName("step")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Step { get; init; }

    /// <summary>
    /// Available options for select fields.
    /// </summary>
    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Options { get; init; }

    /// <summary>
    /// Conditional visibility based on another field's value.
    /// </summary>
    [JsonPropertyName("reliesUpon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReliesUponSchema? ReliesUpon { get; init; }
}
