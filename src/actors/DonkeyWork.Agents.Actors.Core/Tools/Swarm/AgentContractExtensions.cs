using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Models;

namespace DonkeyWork.Agents.Actors.Core.Tools.Swarm;

internal static class AgentContractExtensions
{
    private static readonly HashSet<string> ExcludedChildToolGroups = new(StringComparer.OrdinalIgnoreCase)
    {
        ToolGroupNames.SwarmDelegate,
        ToolGroupNames.SwarmManagement,
    };

    /// <summary>
    /// Creates a copy of the contract with MCP servers, sub-agents, and tool groups inherited from the parent grain context.
    /// Tool groups are only inherited when the child contract has none defined; swarm tools are excluded from inheritance.
    /// If <paramref name="toolGroupOverrides"/> is provided, those groups are used instead of inherited ones.
    /// </summary>
    public static AgentContract WithParentContext(this AgentContract contract, GrainContext context, string[]? toolGroupOverrides = null)
    {
        if (context.McpServers is not { Length: > 0 }
            && context.SubAgents is not { Length: > 0 }
            && context.ToolGroups is not { Length: > 0 })
            return contract;

        var toolGroups = toolGroupOverrides is { Length: > 0 }
            ? toolGroupOverrides
            : contract.ToolGroups.Length > 0
                ? contract.ToolGroups
                : context.ToolGroups.Where(g => !ExcludedChildToolGroups.Contains(g)).ToArray();

        return new AgentContract
        {
            SystemPrompt = contract.SystemPrompt,
            ToolGroups = toolGroups,
            MaxTokens = contract.MaxTokens,
            ThinkingBudgetTokens = contract.ThinkingBudgetTokens,
            Stream = contract.Stream,
            WebSearch = contract.WebSearch,
            WebFetch = contract.WebFetch,
            PersistMessages = contract.PersistMessages,
            Lifecycle = contract.Lifecycle,
            LingerSeconds = contract.LingerSeconds,
            AgentType = contract.AgentType,
            KeyPrefix = contract.KeyPrefix,
            TimeoutSeconds = contract.TimeoutSeconds,
            McpServers = context.McpServers,
            EnableSandbox = contract.EnableSandbox,
            SandboxPodName = contract.SandboxPodName,
            ModelId = contract.ModelId,
            Prompts = contract.Prompts,
            SubAgents = context.SubAgents,
            ReasoningEffort = contract.ReasoningEffort,
        };
    }
}
