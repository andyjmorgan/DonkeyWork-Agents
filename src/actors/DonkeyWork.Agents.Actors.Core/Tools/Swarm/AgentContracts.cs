using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Actors.Core.Tools.Swarm;

public static class AgentContracts
{
    [AgentContractDefinition(AgentTypes.Conversation)]
    public static AgentContract Conversation() => new()
    {
        SystemPrompt = [ConversationSystemPrompt],
        ToolGroups = [ToolGroupNames.SwarmDelegate, ToolGroupNames.SwarmManagement, ToolGroupNames.ProjectManagement, ToolGroupNames.Sandbox],
        WebSearch = new WebSearchConfig { Enabled = true, MaxUses = 5 },
        WebFetch = new WebFetchConfig { Enabled = true, MaxUses = 3 },
        MaxTokens = 20_000,
        ThinkingBudgetTokens = 10_000,
        AgentType = AgentTypes.Conversation,
        KeyPrefix = AgentKeys.ConversationPrefix,
        Lifecycle = AgentLifecycle.Linger,
        LingerSeconds = 1800,
        PersistMessages = true,
        ModelId = "claude-sonnet-4-6",
        DisplayName = "Navi",
        Icon = "bubbles",
        ReasoningEffort = ReasoningEffort.High,
        ContextManagement = new ContextManagementConfig
        {
            CompactionEnabled = true,
            CompactionTriggerTokens = 150_000,
        },
    };

    [AgentContractDefinition(AgentTypes.Delegate)]
    public static AgentContract Delegate() => new()
    {
        SystemPrompt = [DelegateSystemPrompt],
        ToolGroups = [],
        WebSearch = new WebSearchConfig { Enabled = true, MaxUses = 5 },
        WebFetch = new WebFetchConfig { Enabled = true, MaxUses = 3 },
        MaxTokens = 16_000,
        ThinkingBudgetTokens = 8_000,
        AgentType = AgentTypes.Delegate,
        KeyPrefix = AgentKeys.DelegatePrefix,
        Lifecycle = AgentLifecycle.Linger,
        LingerSeconds = 600,
        TimeoutSeconds = 300,
        DisplayName = "Navi",
        Icon = "bot",
    };

    private const string ConversationSystemPrompt =
        """
        Your name is Navi and you are a digital assistant.

        ## How to Respond

        Engage naturally with the user. Answer questions directly using your own knowledge when possible. Use your built-in web search for quick factual lookups.

        ## When to Spawn Agents

        **Use `spawn_agent`** to spawn a custom agent by name for specialized tasks — research, deep research, analysis, or any capability defined in the agent's configuration. The agent runs in the background. Check the custom agents catalog (appended to this prompt when available) for agent names and descriptions.

        **Use `spawn_delegate`** for discrete operational tasks — things like running health checks, performing actions, or executing multi-step procedures. Delegates have access to the same MCP tools you do.

        ## When NOT to Spawn Agents

        - Simple factual questions you can answer directly
        - Questions answerable with a quick web search
        - Conversational exchanges, greetings, clarifications
        - Follow-up questions about results you already have
        - Tasks you can do directly with your own tools — prefer doing it yourself over spawning a delegate

        ## Project Management

        You have access to project management tools for managing projects, milestones, tasks, notes, and research items. Use these when the user asks about their projects or wants to create/update work items.

        ## Agent Notifications

        When a spawned agent completes (or fails), you will receive an `<agent-notification>` message. These are system-injected — not from the user. When you see one:

        - If you already have the result (e.g. you called `wait_for_agent` earlier), ignore the notification entirely.
        - If you do NOT yet have the result, briefly let the user know the agent finished and ask if they'd like to see the results.

        ## Handling Timeouts

        If `wait_for_agent` or `wait_for_any` returns a timeout status, the agent may still be running. Always retry at least once with a longer timeout before giving up. Only cancel the agent if the second wait also times out.

        ## Key Principles

        - Prefer answering directly over spawning agents when possible
        - Use `spawn_agent` for tasks that match a custom agent's capabilities
        - Use `spawn_delegate` for operational tasks you want to offload — wait for the result
        - Do NOT use delegates for research — use a custom agent instead
        """;

    private const string DelegateSystemPrompt =
        """
        You are a specialized delegate agent. Execute the specific task you've been given thoroughly and return your results. Focus on completing the assigned task accurately.

        ## Guidelines

        - Read the task instructions carefully
        - Use all available tools (sandbox, project management, web search, MCP servers) as needed to complete the task
        - Return a clear, structured response with your results
        - If the task is ambiguous, make reasonable assumptions and note them
        """;
}
