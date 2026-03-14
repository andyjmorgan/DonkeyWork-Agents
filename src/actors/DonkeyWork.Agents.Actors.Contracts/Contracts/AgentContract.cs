using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Actors.Contracts.Contracts;

[GenerateSerializer]
public sealed class AgentContract
{
    [Id(0)] public string[] SystemPrompt { get; init; } = [];
    [Id(1)] public string[] ToolGroups { get; init; } = [];
    [Id(2)] public int MaxTokens { get; init; } = 20_000;
    [Id(3)] public int ThinkingBudgetTokens { get; init; } = 10_000;
    [Id(4)] public bool Stream { get; init; } = true;
    [Id(5)] public WebSearchConfig WebSearch { get; init; } = new();
    [Id(6)] public WebFetchConfig WebFetch { get; init; } = new();
    [Id(7)] public bool PersistMessages { get; init; }
    [Id(8)] public AgentLifecycle Lifecycle { get; init; } = AgentLifecycle.Task;
    [Id(9)] public int LingerSeconds { get; init; } = 1800;
    [Id(10)] public string AgentType { get; init; } = "";
    [Id(11)] public string KeyPrefix { get; init; } = "";
    [Id(12)] public int TimeoutSeconds { get; init; } = 1200;
    [Id(13)] public McpServerReference[] McpServers { get; init; } = [];
    [Id(14)] public bool EnableSandbox { get; init; }
    [Id(15)] public string? SandboxPodName { get; init; }
    [Id(16)] public string? ModelId { get; init; }
    [Id(17)] public string[] Prompts { get; init; } = [];
    [Id(18)] public SubAgentReference[] SubAgents { get; init; } = [];
    [Id(19)] public ReasoningEffort? ReasoningEffort { get; init; }
    [Id(20)] public ToolConfig? ToolConfiguration { get; init; }
}
