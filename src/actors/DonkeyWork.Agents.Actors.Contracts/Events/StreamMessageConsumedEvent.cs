using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

/// <summary>
/// Emitted when a queued user message is drained into the active turn instead of
/// starting its own turn. The client uses <see cref="ConsumedTurnId"/> to find the
/// pending user bubble (which never receives a turn_start), clear its cancel-X, and
/// slot it into chronological order. The base <c>TurnId</c> carries the host turn the
/// message was folded into.
/// </summary>
[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamMessageConsumedEvent(
    string AgentKey,
    [property: Id(0)] Guid ConsumedTurnId) : StreamEventBase(AgentKey)
{
    public override string EventType => "message_consumed";
}
