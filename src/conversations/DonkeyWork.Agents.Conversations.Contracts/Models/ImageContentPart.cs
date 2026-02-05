using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// Image content part. References a file stored in blob storage.
/// The image is fetched and converted to base64 during execution.
/// </summary>
public sealed class ImageContentPart : ContentPart
{
    /// <summary>
    /// The ID of the stored file in blob storage.
    /// </summary>
    [JsonPropertyName("fileId")]
    public required Guid FileId { get; set; }

    /// <summary>
    /// The media type (e.g., "image/png", "image/jpeg").
    /// </summary>
    [JsonPropertyName("mediaType")]
    public required string MediaType { get; set; }
}
