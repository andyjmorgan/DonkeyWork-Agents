using DonkeyWork.Agents.Orleans.Contracts.Contracts;
using DonkeyWork.Agents.Orleans.Contracts.Models;

namespace DonkeyWork.Agents.Orleans.Core.Tools.Swarm;

public static class AgentContracts
{
    [AgentContractDefinition("research")]
    public static AgentContract Research() => new()
    {
        SystemPrompt = """
            You are a focused research agent. Your job is to thoroughly research a specific question or topic.

            Guidelines:
            - Use web search to find relevant, up-to-date information
            - Synthesize findings into a clear, comprehensive answer
            - Cite sources when possible
            - If the question is ambiguous, research the most likely interpretation
            - Focus on accuracy and completeness
            - Return your findings as a well-structured response
            """,
        ToolGroups = [],
        WebSearch = new WebSearchConfig { Enabled = true, MaxUses = 10 },
        WebFetch = new WebFetchConfig { Enabled = true, MaxUses = 5 },
        MaxTokens = 16_000,
        ThinkingBudgetTokens = 8_000,
        AgentType = "research",
        KeyPrefix = AgentKeys.ResearchPrefix,
        Lifecycle = AgentLifecycle.Task,
        TimeoutSeconds = 300,
    };

    [AgentContractDefinition("deep_research")]
    public static AgentContract DeepResearch() => new()
    {
        SystemPrompt = """
            You are a deep research agent capable of spawning sub-researchers for comprehensive investigation.

            Guidelines:
            - Break complex topics into sub-questions
            - Spawn research agents for each sub-question using the spawn_researcher tool
            - Use wait_for_any or wait_for_agent to collect results
            - Synthesize all findings into a comprehensive, well-structured report
            - Ensure coverage of multiple perspectives and sources
            - Cite sources throughout your report
            - Return a thorough, publication-quality analysis
            """,
        ToolGroups = ["swarm_spawn", "swarm_management"],
        WebSearch = new WebSearchConfig { Enabled = true, MaxUses = 10 },
        WebFetch = new WebFetchConfig { Enabled = true, MaxUses = 5 },
        MaxTokens = 32_000,
        ThinkingBudgetTokens = 16_000,
        AgentType = "deep_research",
        KeyPrefix = AgentKeys.DeepResearchPrefix,
        Lifecycle = AgentLifecycle.Task,
        TimeoutSeconds = 600,
    };

    [AgentContractDefinition("conversation")]
    public static AgentContract Conversation() => new()
    {
        SystemPrompt = """
            You are a helpful conversational assistant. Engage naturally with the user, answer questions,
            and help with tasks. You can spawn research agents for complex questions and delegate tasks
            to specialized agents when appropriate.
            """,
        ToolGroups = ["swarm_spawn", "swarm_management"],
        WebSearch = new WebSearchConfig { Enabled = true, MaxUses = 5 },
        WebFetch = new WebFetchConfig { Enabled = true, MaxUses = 3 },
        MaxTokens = 20_000,
        ThinkingBudgetTokens = 10_000,
        AgentType = "conversation",
        KeyPrefix = AgentKeys.ConversationPrefix,
        Lifecycle = AgentLifecycle.Linger,
        LingerSeconds = 1800,
        PersistMessages = true,
    };

    [AgentContractDefinition("delegate")]
    public static AgentContract Delegate() => new()
    {
        SystemPrompt = """
            You are a specialized delegate agent. Execute the specific task you've been given thoroughly
            and return your results. Focus on completing the assigned task accurately.
            """,
        ToolGroups = [],
        WebSearch = new WebSearchConfig { Enabled = true, MaxUses = 5 },
        WebFetch = new WebFetchConfig { Enabled = true, MaxUses = 3 },
        MaxTokens = 16_000,
        ThinkingBudgetTokens = 8_000,
        AgentType = "delegate",
        KeyPrefix = AgentKeys.DelegatePrefix,
        Lifecycle = AgentLifecycle.Task,
        TimeoutSeconds = 300,
    };
}
