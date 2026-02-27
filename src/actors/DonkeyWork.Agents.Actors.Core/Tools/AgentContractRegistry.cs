using System.Collections.Frozen;
using System.Reflection;
using DonkeyWork.Agents.Actors.Contracts.Contracts;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Tools;

public sealed class AgentContractRegistry
{
    private readonly FrozenDictionary<string, AgentContractDescriptor> _contracts;

    public AgentContractRegistry(ILogger<AgentContractRegistry> logger, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = [typeof(AgentContractRegistry).Assembly];
        }

        var contracts = new Dictionary<string, AgentContractDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assemblies)
        {
            ScanAssembly(assembly, contracts, logger);
        }

        _contracts = contracts.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        logger.LogInformation("AgentContractRegistry initialized with {ContractCount} contracts", _contracts.Count);
    }

    public AgentContractDescriptor? GetContract(string name) =>
        _contracts.TryGetValue(name, out var descriptor) ? descriptor : null;

    public IReadOnlyList<AgentContractDescriptor> GetAllContracts() =>
        _contracts.Values.ToList();

    public bool HasContract(string name) =>
        _contracts.ContainsKey(name);

    private static void ScanAssembly(
        Assembly assembly,
        Dictionary<string, AgentContractDescriptor> contracts,
        ILogger logger)
    {
        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = method.GetCustomAttribute<AgentContractDefinitionAttribute>();
                if (attr is null)
                {
                    continue;
                }

                if (method.ReturnType != typeof(AgentContract))
                {
                    logger.LogWarning(
                        "Method {Type}.{Method} has [AgentContractDefinition] but does not return AgentContract",
                        type.FullName,
                        method.Name);
                    continue;
                }

                if (method.GetParameters().Length != 0)
                {
                    logger.LogWarning(
                        "Method {Type}.{Method} has [AgentContractDefinition] but has parameters",
                        type.FullName,
                        method.Name);
                    continue;
                }

                var contract = (AgentContract)method.Invoke(null, null)!;
                var description = method.Name;

                if (contracts.ContainsKey(attr.Name))
                {
                    logger.LogWarning("Duplicate contract name '{Name}' from {Type}.{Method}. Skipping.",
                        attr.Name,
                        type.FullName,
                        method.Name);
                    continue;
                }

                contracts[attr.Name] = new AgentContractDescriptor(attr.Name, description, contract);
            }
        }
    }
}
