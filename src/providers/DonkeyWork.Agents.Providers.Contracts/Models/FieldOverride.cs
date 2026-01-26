using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Models;

/// <summary>
/// Overrides for a configuration field's defaults, specified per-model in models.json.
/// </summary>
public sealed class FieldOverride
{
    [JsonPropertyName("min")]
    public double? Min { get; init; }

    [JsonPropertyName("max")]
    public double? Max { get; init; }

    [JsonPropertyName("step")]
    public double? Step { get; init; }

    [JsonPropertyName("default")]
    public object? Default { get; init; }

    [JsonPropertyName("hidden")]
    public bool? Hidden { get; init; }
}
