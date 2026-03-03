using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// Image content part. References a file stored in blob storage by object key.
/// The image is fetched and converted to base64 during execution.
/// </summary>
public sealed class ImageContentPart : ContentPart
{
    /// <summary>
    /// The object key of the file in blob storage (relative to user namespace).
    /// </summary>
    [JsonPropertyName("objectKey")]
    public required string ObjectKey { get; set; }

    /// <summary>
    /// The media type (e.g., "image/png", "image/jpeg").
    /// </summary>
    [JsonPropertyName("mediaType")]
    public required string MediaType { get; set; }
}
