using DonkeyWork.Agents.Actors.Contracts.Events;

namespace DonkeyWork.Agents.Actors.Contracts.Services;

/// <summary>
/// Publishes stream events to JetStream so any subscriber (WebSocket session, replay)
/// can receive them without a direct grain observer reference.
/// </summary>
public interface IAgentEventPublisher
{
    Task PublishAsync(StreamEventBase evt, string conversationId, CancellationToken ct = default);
}
