namespace DonkeyWork.Agents.Orleans.Contracts.Contracts;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AgentContractDefinitionAttribute : Attribute
{
    public string Name { get; }

    public AgentContractDefinitionAttribute(string name) => Name = name;
}
