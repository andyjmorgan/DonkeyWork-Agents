using DonkeyWork.Agents.Common.MessageBus.Payloads;

namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public abstract record StreamEventBase([property: Id(0)] string AgentKey) : IPayload
{
    public abstract string EventType { get; }

    [Id(1)]
    public Guid TurnId { get; init; }

    string IPayload.Discriminator => GetType().Name;
}
