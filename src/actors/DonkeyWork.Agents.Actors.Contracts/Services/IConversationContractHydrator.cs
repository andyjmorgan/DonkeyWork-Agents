using DonkeyWork.Agents.Actors.Contracts.Contracts;

namespace DonkeyWork.Agents.Actors.Contracts.Services;

public interface IConversationContractHydrator
{
    Task<AgentContract> HydrateAsync(AgentContract baseContract, CancellationToken ct = default);
}
