using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Sdk.Models.Schema;

namespace DonkeyWork.Agents.Providers.Contracts.Models.Schema;

/// <summary>
/// Schema definition for a single configuration field.
/// </summary>
public sealed class ConfigFieldSchema
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonPropertyName("controlType")]
    public required FieldControlType ControlType { get; init; }

    [JsonPropertyName("propertyType")]
    public required string PropertyType { get; init; }

    [JsonPropertyName("order")]
    public int Order { get; init; }

    [JsonPropertyName("group")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Group { get; init; }

    [JsonPropertyName("tab")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tab { get; init; }

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("resolvable")]
    public bool Resolvable { get; init; }

    [JsonPropertyName("min")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Min { get; init; }

    [JsonPropertyName("max")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Max { get; init; }

    [JsonPropertyName("step")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Step { get; init; }

    [JsonPropertyName("default")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? DefaultValue { get; init; }

    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Options { get; init; }

    [JsonPropertyName("dependsOn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FieldDependency>? DependsOn { get; init; }

    [JsonPropertyName("reliesUpon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReliesUponSchema? ReliesUpon { get; init; }

    [JsonPropertyName("credentialTypes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? CredentialTypes { get; init; }
}
