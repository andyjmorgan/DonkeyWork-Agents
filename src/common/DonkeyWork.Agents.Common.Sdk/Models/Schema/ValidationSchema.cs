using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Common.Sdk.Models.Schema;

/// <summary>
/// Schema for field validation rules.
/// </summary>
public sealed class ValidationSchema
{
    /// <summary>
    /// Minimum numeric value.
    /// </summary>
    [JsonPropertyName("min")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Min { get; init; }

    /// <summary>
    /// Maximum numeric value.
    /// </summary>
    [JsonPropertyName("max")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Max { get; init; }

    /// <summary>
    /// Step increment for numeric values.
    /// </summary>
    [JsonPropertyName("step")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Step { get; init; }

    /// <summary>
    /// Minimum string length.
    /// </summary>
    [JsonPropertyName("minLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinLength { get; init; }

    /// <summary>
    /// Maximum string length.
    /// </summary>
    [JsonPropertyName("maxLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxLength { get; init; }

    /// <summary>
    /// Regex pattern for validation.
    /// </summary>
    [JsonPropertyName("pattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Pattern { get; init; }

    /// <summary>
    /// Custom error message for validation failures.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorMessage { get; init; }
}
