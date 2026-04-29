using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Contracts.Models;
using Orleans.Concurrency;

namespace DonkeyWork.Agents.Actors.Contracts.Grains;

public interface IAgentGrain : IGrainWithStringKey
{
    Task<AgentResult> RunAsync(AgentContract contract, string input);

    [AlwaysInterleave]
    Task CancelAsync();

    [AlwaysInterleave]
    Task<IReadOnlyList<InternalMessage>> GetMessagesAsync();

    [AlwaysInterleave]
    Task DeliverMessageAsync(AgentMessage message);
}
