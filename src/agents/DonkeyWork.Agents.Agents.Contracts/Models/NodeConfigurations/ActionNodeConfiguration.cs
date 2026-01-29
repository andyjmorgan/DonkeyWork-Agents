using System.Text.Json;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;

/// <summary>
/// Configuration for an Action node that executes an action provider.
/// </summary>
public sealed class ActionNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// The action type identifier (e.g., "http_request").
    /// </summary>
    [JsonPropertyName("actionType")]
    public required string ActionType { get; init; }

    /// <summary>
    /// Display name of the action (for UI purposes).
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>
    /// Action parameters as a JSON dictionary.
    /// The structure depends on the action type's parameter schema.
    /// </summary>
    [JsonPropertyName("parameters")]
    public required JsonElement Parameters { get; init; }

    /// <summary>
    /// Deserializes the parameters to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The deserialized parameters.</returns>
    public T GetParameters<T>() where T : class
        => JsonSerializer.Deserialize<T>(Parameters.GetRawText())
           ?? throw new InvalidOperationException($"Failed to deserialize parameters to {typeof(T).Name}");
}
