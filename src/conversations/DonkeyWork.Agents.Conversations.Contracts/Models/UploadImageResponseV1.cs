using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// Response from uploading an image to a conversation.
/// </summary>
public sealed class UploadImageResponseV1
{
    /// <summary>
    /// The object key to use in ImageContentPart (relative to user namespace).
    /// </summary>
    [JsonPropertyName("objectKey")]
    public required string ObjectKey { get; init; }

    /// <summary>
    /// The original file name.
    /// </summary>
    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    /// <summary>
    /// The content type of the uploaded file.
    /// </summary>
    [JsonPropertyName("contentType")]
    public required string ContentType { get; init; }

    /// <summary>
    /// The size of the file in bytes.
    /// </summary>
    [JsonPropertyName("sizeBytes")]
    public required long SizeBytes { get; init; }
}
