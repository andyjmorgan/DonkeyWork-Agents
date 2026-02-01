using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Common.Sdk.Models.Schema;

/// <summary>
/// Unified schema definition for a configurable field.
/// Used by both Actions and Model configurations.
/// </summary>
public sealed class FieldSchema
{
    /// <summary>
    /// The property name (camelCase).
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Display label for the field in the UI.
    /// </summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Optional description explaining the field's purpose.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    /// <summary>
    /// The underlying data type (string, number, boolean, enum, credential).
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// The UI control type (text, textarea, slider, dropdown, checkbox, credential, etc.).
    /// </summary>
    [JsonPropertyName("controlType")]
    public required string ControlType { get; init; }

    /// <summary>
    /// Whether this field supports Resolvable&lt;T&gt; (variable expressions).
    /// </summary>
    [JsonPropertyName("resolvable")]
    public bool Resolvable { get; init; }

    /// <summary>
    /// Whether this field supports {{variable}} syntax.
    /// </summary>
    [JsonPropertyName("supportsVariables")]
    public bool SupportsVariables { get; init; }

    /// <summary>
    /// Display order within the form (lower values appear first).
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; init; }

    /// <summary>
    /// The tab this field belongs to.
    /// </summary>
    [JsonPropertyName("tab")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tab { get; init; }

    /// <summary>
    /// Sub-group within a tab for further organization.
    /// </summary>
    [JsonPropertyName("group")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Group { get; init; }

    /// <summary>
    /// Whether this field is required.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; init; }

    /// <summary>
    /// The default value for the field.
    /// </summary>
    [JsonPropertyName("default")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Placeholder text for the field input.
    /// </summary>
    [JsonPropertyName("placeholder")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Placeholder { get; init; }

    /// <summary>
    /// Validation rules for the field.
    /// </summary>
    [JsonPropertyName("validation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ValidationSchema? Validation { get; init; }

    /// <summary>
    /// Available options for dropdown/select fields.
    /// </summary>
    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<OptionSchema>? Options { get; init; }

    /// <summary>
    /// Conditional visibility based on another field's value.
    /// </summary>
    [JsonPropertyName("reliesUpon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReliesUponSchema? ReliesUpon { get; init; }

    /// <summary>
    /// Legacy dependency information for backward compatibility.
    /// </summary>
    [JsonPropertyName("dependsOn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<DependencySchema>? DependsOn { get; init; }

    /// <summary>
    /// The model capability required for this field to be shown.
    /// </summary>
    [JsonPropertyName("requiresCapability")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RequiresCapability { get; init; }

    /// <summary>
    /// Credential types this field accepts.
    /// </summary>
    [JsonPropertyName("credentialTypes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? CredentialTypes { get; init; }
}
