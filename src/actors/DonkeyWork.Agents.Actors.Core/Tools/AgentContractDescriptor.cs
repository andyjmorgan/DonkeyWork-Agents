using DonkeyWork.Agents.Actors.Contracts.Contracts;

namespace DonkeyWork.Agents.Actors.Core.Tools;

public sealed record AgentContractDescriptor(string Name, string Description, AgentContract Contract);
