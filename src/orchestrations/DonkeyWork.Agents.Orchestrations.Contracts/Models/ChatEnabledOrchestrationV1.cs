using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Summary of a chat-enabled orchestration for the agent selector.
/// </summary>
public sealed class ChatEnabledOrchestrationV1
{
    /// <summary>
    /// Orchestration ID.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>
    /// Orchestration name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Description from the Chat interface configuration.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
