using DonkeyWork.Agents.Orleans.Contracts.Contracts;

namespace DonkeyWork.Agents.Orleans.Core.Tools;

public sealed record AgentContractDescriptor(string Name, string Description, AgentContract Contract);
