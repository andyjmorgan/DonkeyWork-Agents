namespace DonkeyWork.Agents.Actors.Contracts.Contracts;

public interface IAgentContractRegistry
{
    AgentContractDescriptor? GetContract(string name);

    IReadOnlyList<AgentContractDescriptor> GetAllContracts();

    bool HasContract(string name);
}
