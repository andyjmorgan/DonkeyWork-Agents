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
    /// Creates a copy of the contract with the specified sandbox pod name.
    /// </summary>
    public static AgentContract WithSandboxPodName(this AgentContract contract, string podName) => new()
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
        McpServers = contract.McpServers,
        EnableSandbox = contract.EnableSandbox,
        SandboxPodName = podName,
        ModelId = contract.ModelId,
        Prompts = contract.Prompts,
        SubAgents = contract.SubAgents,
        ReasoningEffort = contract.ReasoningEffort,
    };

    /// <summary>
    /// Creates a copy of the contract inheriting MCP servers, sub-agents, and tool groups from the parent grain context.
    /// If the contract already defines tool groups (e.g. custom agents), those are kept.
    /// Otherwise (e.g. delegates), all parent tool groups are inherited (except swarm spawning tools).
    /// MCP servers and sub-agents are always inherited from the parent.
    /// </summary>
    public static AgentContract WithParentContext(this AgentContract contract, GrainContext context)
    {
        var toolGroups = contract.ToolGroups.Length > 0
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
