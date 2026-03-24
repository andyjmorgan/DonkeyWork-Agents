using System.Text.Json;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Actors.Contracts.Messages;

[GenerateSerializer]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(InternalTextBlock), nameof(InternalTextBlock))]
[JsonDerivedType(typeof(InternalToolUseBlock), nameof(InternalToolUseBlock))]
[JsonDerivedType(typeof(InternalServerToolUseBlock), nameof(InternalServerToolUseBlock))]
[JsonDerivedType(typeof(InternalWebSearchResultBlock), nameof(InternalWebSearchResultBlock))]
[JsonDerivedType(typeof(InternalWebFetchToolResultBlock), nameof(InternalWebFetchToolResultBlock))]
[JsonDerivedType(typeof(InternalThinkingBlock), nameof(InternalThinkingBlock))]
[JsonDerivedType(typeof(InternalCitationBlock), nameof(InternalCitationBlock))]
[JsonDerivedType(typeof(InternalToolSearchResultBlock), nameof(InternalToolSearchResultBlock))]
[JsonDerivedType(typeof(InternalCompactionBlock), nameof(InternalCompactionBlock))]
public abstract record InternalContentBlock;

[GenerateSerializer]
public sealed record InternalTextBlock([property: Id(0)] string Text) : InternalContentBlock;

[GenerateSerializer]
public sealed record InternalToolUseBlock([property: Id(0)] string Id, [property: Id(1)] string Name, [property: Id(2)] JsonElement Input) : InternalContentBlock;

[GenerateSerializer]
public sealed record InternalServerToolUseBlock([property: Id(0)] string Id, [property: Id(1)] string Name, [property: Id(2)] JsonElement Input) : InternalContentBlock;

[GenerateSerializer]
public sealed record InternalWebSearchResultBlock([property: Id(0)] string ToolUseId, [property: Id(1)] string RawJson) : InternalContentBlock;

[GenerateSerializer]
public sealed record InternalWebFetchToolResultBlock([property: Id(0)] string ToolUseId, [property: Id(1)] string RawJson) : InternalContentBlock;

[GenerateSerializer]
public sealed record InternalThinkingBlock([property: Id(0)] string Text, [property: Id(1)] string? Signature) : InternalContentBlock;

[GenerateSerializer]
public sealed record InternalCitationBlock([property: Id(0)] string Title, [property: Id(1)] string Url, [property: Id(2)] string CitedText) : InternalContentBlock;

[GenerateSerializer]
public sealed record InternalToolSearchResultBlock([property: Id(0)] string ToolUseId, [property: Id(1)] string RawJson) : InternalContentBlock;

[GenerateSerializer]
public sealed record InternalCompactionBlock([property: Id(0)] string? Summary) : InternalContentBlock;
