using DonkeyWork.Agents.Orleans.Contracts.Contracts;
using DonkeyWork.Agents.Orleans.Contracts.Messages;
using DonkeyWork.Agents.Orleans.Contracts.Models;
using Orleans.Concurrency;

namespace DonkeyWork.Agents.Orleans.Contracts.Grains;

public interface IAgentGrain : IGrainWithStringKey
{
    Task<AgentResult> RunAsync(AgentContract contract, string input, IAgentResponseObserver? observer);

    [AlwaysInterleave]
    Task CancelAsync();

    [AlwaysInterleave]
    Task<IReadOnlyList<InternalMessage>> GetMessagesAsync();
}
