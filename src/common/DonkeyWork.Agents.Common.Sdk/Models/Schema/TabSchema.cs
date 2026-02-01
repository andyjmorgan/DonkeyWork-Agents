using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Common.Sdk.Models.Schema;

/// <summary>
/// Schema definition for a tab in the configuration UI.
/// </summary>
public sealed class TabSchema
{
    /// <summary>
    /// The name/identifier of the tab.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Display order of the tab (lower values appear first).
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; init; }

    /// <summary>
    /// Optional icon name for the tab.
    /// </summary>
    [JsonPropertyName("icon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; init; }
}
