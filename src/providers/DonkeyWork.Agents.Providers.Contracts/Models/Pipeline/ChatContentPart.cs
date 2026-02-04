using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;

/// <summary>
/// Base class for chat message content parts.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextChatContentPart), "text")]
[JsonDerivedType(typeof(ImageChatContentPart), "image")]
[JsonDerivedType(typeof(ThinkingChatContentPart), "thinking")]
public abstract class ChatContentPart
{
}

/// <summary>
/// Text content part.
/// </summary>
public sealed class TextChatContentPart : ChatContentPart
{
    /// <summary>
    /// The text content.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; set; }
}

/// <summary>
/// Image content part.
/// </summary>
public sealed class ImageChatContentPart : ChatContentPart
{
    /// <summary>
    /// The image source type.
    /// </summary>
    [JsonPropertyName("source_type")]
    public required string SourceType { get; set; }

    /// <summary>
    /// The media type (e.g., "image/png", "image/jpeg").
    /// </summary>
    [JsonPropertyName("media_type")]
    public required string MediaType { get; set; }

    /// <summary>
    /// The image data (base64 encoded for base64 source type, URL for url source type).
    /// </summary>
    [JsonPropertyName("data")]
    public required string Data { get; set; }
}

/// <summary>
/// Thinking/reasoning content part.
/// </summary>
public sealed class ThinkingChatContentPart : ChatContentPart
{
    /// <summary>
    /// The thinking content.
    /// </summary>
    [JsonPropertyName("thinking")]
    public required string Thinking { get; set; }

    /// <summary>
    /// Whether this is encrypted reasoning (opaque, not human-readable).
    /// </summary>
    [JsonPropertyName("is_encrypted")]
    public bool IsEncrypted { get; set; }
}
