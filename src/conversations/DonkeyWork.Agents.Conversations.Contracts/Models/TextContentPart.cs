using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// Text content part.
/// </summary>
public sealed class TextContentPart : ContentPart
{
    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "text";

    /// <summary>
    /// The text content.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; set; }
}
