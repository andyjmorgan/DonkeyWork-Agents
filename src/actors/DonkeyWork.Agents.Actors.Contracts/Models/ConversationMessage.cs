namespace DonkeyWork.Agents.Actors.Contracts.Models;

[GenerateSerializer]
public abstract record ConversationMessage([property: Id(0)] DateTimeOffset Timestamp);

[GenerateSerializer]
public sealed record UserConversationMessage(
    [property: Id(1)] string Text,
    [property: Id(2)] Guid TurnId,
    DateTimeOffset Timestamp) : ConversationMessage(Timestamp);

[GenerateSerializer]
public sealed record AgentResultConversationMessage(
    [property: Id(1)] string AgentKey,
    [property: Id(2)] string Label,
    [property: Id(3)] AgentResult? Result,
    [property: Id(4)] bool IsError,
    DateTimeOffset Timestamp) : ConversationMessage(Timestamp);

[GenerateSerializer]
public sealed record AgentMessageConversationMessage(
    [property: Id(1)] AgentMessage Message,
    DateTimeOffset Timestamp) : ConversationMessage(Timestamp);
