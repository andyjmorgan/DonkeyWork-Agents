using DonkeyWork.Agents.Actors.Contracts.Contracts;

namespace DonkeyWork.Agents.Actors.Core.Tools.Swarm;

internal static class AgentContractExtensions
{
    /// <summary>
    /// Creates a copy of the contract with MCP servers and sub-agents inherited from the parent grain context.
    /// </summary>
    public static AgentContract WithParentContext(this AgentContract contract, GrainContext context)
    {
        if (context.McpServers is not { Length: > 0 } && context.SubAgents is not { Length: > 0 })
            return contract;

        return new AgentContract
        {
            SystemPrompt = contract.SystemPrompt,
            ToolGroups = contract.ToolGroups,
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
