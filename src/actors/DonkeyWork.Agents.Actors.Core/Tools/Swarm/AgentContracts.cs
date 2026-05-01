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
        ToolGroups = [ToolGroupNames.SwarmDelegate, ToolGroupNames.SwarmManagement, ToolGroupNames.ProjectManagement, ToolGroupNames.Sandbox, ToolGroupNames.Scheduling, ToolGroupNames.AudioCollections],
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
        ModelId = "claude-sonnet-4-6",
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

        Both `spawn_agent` and `spawn_delegate` are fire-and-forget. Do NOT block with `wait_for_agent` unless the task is very short-lived and the user is actively waiting for an immediate result (e.g. a quick delegate action). For any longer-running task, spawn and continue the conversation — results will arrive via `<agent-notification>`.

        Use `send_message` for mid-task coordination. The agent's reply arrives automatically as an `<agent-message>` tag — do NOT then call `wait_for_agent`.

        ## When NOT to Spawn Agents

        - Simple factual questions you can answer directly
        - Questions answerable with a quick web search
        - Conversational exchanges, greetings, clarifications
        - Follow-up questions about results you already have
        - Tasks you can do directly with your own tools — prefer doing it yourself over spawning a delegate

        ## Project Management

        You have access to project management tools for managing projects, milestones, tasks, notes, and research items. Use these when the user asks about their projects or wants to create/update work items.

        ## Audio Collections

        You have access to audio collection tools for browsing and organizing the user's library of AI-generated audio recordings. Collections are folders of recordings (think chapters of a podcast or daily news feed); recordings can also live unfiled. Use these tools to list/get/create/update/delete collections, list recordings (flat, scoped to a collection, or unfiled-only), and to move, rename, or delete individual recordings. Recording *creation* is driven by orchestrations — do not attempt to create recordings yourself.

        ## Agent Notifications

        When a spawned agent completes (or fails), you will receive an `<agent-notification>` message. These are system-injected — not from the user. When you see one:

        - If you already have the result (e.g. from an earlier `wait_for_agent` call), ignore the notification entirely.
        - If you do NOT yet have the result, briefly let the user know the agent finished and ask if they'd like to see the results.

        ## Handling Timeouts

        If you do use `wait_for_agent` (for a short synchronous operation) and it times out, do NOT retry with a longer timeout — the agent is still running and its result will arrive via `<agent-notification>`. Let it run and continue the conversation.

        ## Key Principles

        - Prefer answering directly over spawning agents when possible
        - Use `spawn_agent` for tasks that match a custom agent's capabilities
        - Use `spawn_delegate` for operational tasks you want to offload — prefer fire-and-forget over blocking
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
