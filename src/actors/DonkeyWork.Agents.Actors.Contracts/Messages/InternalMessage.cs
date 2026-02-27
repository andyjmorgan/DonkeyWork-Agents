using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Actors.Contracts.Messages;

[GenerateSerializer]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(InternalContentMessage), nameof(InternalContentMessage))]
[JsonDerivedType(typeof(InternalToolResultMessage), nameof(InternalToolResultMessage))]
[JsonDerivedType(typeof(InternalAssistantMessage), nameof(InternalAssistantMessage))]
public abstract class InternalMessage
{
    [Id(0)] public required InternalMessageRole Role { get; set; }
}
