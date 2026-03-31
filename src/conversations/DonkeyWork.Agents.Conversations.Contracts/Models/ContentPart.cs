using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// Base class for message content parts.
/// Uses polymorphic JSON serialization with "type" discriminator for API compatibility.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextContentPart), "text")]
[JsonDerivedType(typeof(ImageContentPart), "image")]
[JsonDerivedType(typeof(AudioContentPart), "audio")]
public abstract class ContentPart
{
}
